using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Push;
using FootballManager.Infrastructure.Persistence;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPushSubscription = Lib.Net.Http.WebPush.PushSubscription;

namespace FootballManager.Infrastructure.Push;

public interface IWebPushSender
{
    Task SendAsync(PushDispatchTarget target, PushPayloadDto payload, CancellationToken cancellationToken = default);
}

public sealed class WebPushSender : IWebPushSender
{
    private readonly PushServiceClient _client;
    private readonly FootballManagerDbContext _db;
    private readonly WebPushOptions _options;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(
        IHttpClientFactory httpClientFactory,
        FootballManagerDbContext db,
        IOptions<WebPushOptions> options,
        ILogger<WebPushSender> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
        _client = new PushServiceClient(httpClientFactory.CreateClient(nameof(WebPushSender)))
        {
            DefaultAuthentication = new VapidAuthentication(_options.PublicKey, _options.PrivateKey)
            {
                Subject = _options.Subject
            }
        };
    }

    public async Task SendAsync(PushDispatchTarget target, PushPayloadDto payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicKey) || string.IsNullOrWhiteSpace(_options.PrivateKey))
        {
            _logger.LogWarning("WebPush keys are not configured; skipping send.");
            return;
        }

        var absoluteUrl = Absolutize(payload.Url);
        var icon = Absolutize(payload.Icon ?? _options.DefaultIconPath);
        var badge = Absolutize(payload.Badge ?? _options.DefaultBadgePath);

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            title = payload.Title,
            body = payload.Body,
            icon,
            badge,
            url = absoluteUrl
        });

        var subscription = new WebPushSubscription { Endpoint = target.Endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, target.P256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, target.Auth);

        var message = new PushMessage(json)
        {
            Urgency = PushMessageUrgency.Normal
        };

        try
        {
            await _client.RequestPushMessageDeliveryAsync(subscription, message, cancellationToken);
            var entity = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Id == target.SubscriptionId, cancellationToken);
            entity?.Touch();
            if (entity != null)
                await _db.SaveChangesAsync(cancellationToken);
        }
        catch (PushServiceClientException ex) when ((int)ex.StatusCode is 404 or 410)
        {
            _logger.LogInformation("Push subscription {Id} gone ({Status}); deactivating.", target.SubscriptionId, ex.StatusCode);
            var entity = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Id == target.SubscriptionId, cancellationToken);
            if (entity != null)
            {
                entity.Deactivate();
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deliver push to subscription {Id}", target.SubscriptionId);
        }
    }

    private string Absolutize(string pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl)) return _options.PublicBaseUrl;
        if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return pathOrUrl;
        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        return pathOrUrl.StartsWith('/') ? baseUrl + pathOrUrl : baseUrl + "/" + pathOrUrl;
    }
}
