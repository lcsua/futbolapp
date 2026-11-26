using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace FootballManager.Api.Services.Public;

/// <summary>
/// One Monte Carlo run per championship fingerprint. A second request for the same
/// data waits for the run already in progress instead of starting another.
/// The cached value is replaced only when results, fixtures or the model version change.
/// Nothing is written to the database. A future per-round history can keep these snapshots.
/// </summary>
public sealed class CachedPrediction
{
    public required FootballManager.Application.Predictions.DivisionOutlook Outlook { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
}

public sealed class PredictionCache
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    public PredictionCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<T> factory,
        TimeSpan lifetime,
        CancellationToken cancellationToken) where T : class
    {
        if (_cache.TryGetValue(key, out T? cached) && cached != null)
            return cached;

        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(key, out cached) && cached != null)
                return cached;

            var created = await Task.Run(factory, cancellationToken);
            _cache.Set(key, created, lifetime);
            return created;
        }
        finally
        {
            gate.Release();
        }
    }
}
