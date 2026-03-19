using FootballManager.Application;
using FootballManager.Application.Interfaces;
using FootballManager.Infrastructure;
using FootballManager.Api.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDevTokenStore, DevTokenStore>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
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
app.UseMiddleware<FootballManager.Api.Middleware.DevAuthMiddleware>();
app.UseMiddleware<FootballManager.Api.Middleware.ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
