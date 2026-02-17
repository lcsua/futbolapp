using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FootballManager.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception has occurred.");

            var statusCode = HttpStatusCode.InternalServerError;
            var response = new { error = exception.Message };

            switch (exception)
            {
                case ArgumentException _:
                case LeagueAlreadyExistsException _:
                    statusCode = HttpStatusCode.BadRequest;
                    break;
                case ForbiddenAccessException _:
                    statusCode = HttpStatusCode.Forbidden;
                    break;
                case KeyNotFoundException _:
                    statusCode = HttpStatusCode.NotFound;
                    break;
                // Add more custom exceptions here
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
