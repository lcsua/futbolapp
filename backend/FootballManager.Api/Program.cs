using FootballManager.Application;
using FootballManager.Application.Interfaces;
using FootballManager.Infrastructure;
using FootballManager.Api.Auth;
using FootballManager.Api.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDevTokenStore, DevTokenStore>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddScoped<LeaguePermissionFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<LeaguePermissionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("push", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 60;
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddScoped<FootballManager.Api.Services.Public.PublicLeagueService>();
builder.Services.AddScoped<FootballManager.Api.Services.Public.PublicTeamService>();
builder.Services.AddScoped<FootballManager.Api.Services.Public.PublicMatchService>();
builder.Services.AddScoped<FootballManager.Api.Services.Public.PublicStructuredService>();
builder.Services.AddScoped<FootballManager.Api.Services.LeagueLogoMigrationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(wwwroot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwroot),
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/uploads"))
        {
            ctx.Context.Response.Headers.CacheControl = "public, max-age=86400";
        }
    }
});

app.UseRateLimiter();
app.UseMiddleware<FootballManager.Api.Middleware.DevAuthMiddleware>();
app.UseMiddleware<FootballManager.Api.Middleware.ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
