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
