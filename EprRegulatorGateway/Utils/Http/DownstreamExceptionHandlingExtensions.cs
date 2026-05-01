using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace EprRegulatorGateway.Utils.Http;

[ExcludeFromCodeCoverage]
public static class DownstreamExceptionHandlingExtensions
{
    public static IApplicationBuilder UseDownstreamExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(exceptionApp =>
        {
            exceptionApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = feature?.Error;

                if (exception is null || context.Response.HasStarted)
                {
                    return;
                }

                // If the client cancelled the request, avoid translating into a 5xx.
                if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
                {
                    return;
                }

                var (statusCode, title, detail) = Map(exception);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail,
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
            });
        });
    }

    private static (int statusCode, string title, string detail) Map(Exception exception)
    {
        if (exception is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode == HttpStatusCode.NotFound)
            {
                return (StatusCodes.Status404NotFound, "Not Found", "Resource was not found.");
            }

            return (StatusCodes.Status502BadGateway, "Bad Gateway", "Downstream service returned an error.");
        }

        // HttpClient timeouts typically surface as TaskCanceledException / OperationCanceledException.
        if (exception is TaskCanceledException or TimeoutException)
        {
            return (StatusCodes.Status504GatewayTimeout, "Gateway Timeout", "Downstream service did not respond in time.");
        }

        return (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.");
    }
}

