using FootballManager.Application;
using FootballManager.Application.Interfaces;
using FootballManager.Infrastructure;
using FootballManager.Api.Auth;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDevTokenStore, DevTokenStore>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<FootballManager.Api.Services.Public.PublicLeagueService>();
builder.Services.AddScoped<FootballManager.Api.Services.Public.PublicTeamService>();
builder.Services.AddScoped<FootballManager.Api.Services.Public.PublicMatchService>();
builder.Services.AddScoped<FootballManager.Api.Services.Public.PublicStructuredService>();

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

app.UseMiddleware<FootballManager.Api.Middleware.DevAuthMiddleware>();
app.UseMiddleware<FootballManager.Api.Middleware.ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
