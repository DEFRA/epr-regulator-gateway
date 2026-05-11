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
                if (context.RequestAborted.IsCancellationRequested && ExceptionChainContainsOperationCanceled(exception))
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
        HttpRequestException? transportHttpEx = null;

        for (var e = exception; e is not null; e = e.InnerException)
        {
            switch (e)
            {
                case HttpRequestException { StatusCode: HttpStatusCode.NotFound }:
                    return (StatusCodes.Status404NotFound, "Not Found", "Resource was not found.");

                case HttpRequestException { StatusCode: not null }:
                    return (StatusCodes.Status502BadGateway, "Bad Gateway", "Downstream service returned an error.");

                case HttpRequestException httpEx:
                    transportHttpEx ??= httpEx;
                    break;

                case TaskCanceledException or TimeoutException:
                    return (StatusCodes.Status504GatewayTimeout, "Gateway Timeout", "Downstream service did not respond in time.");
            }
        }

        if (transportHttpEx is not null)
        {
            return (StatusCodes.Status502BadGateway, "Bad Gateway", "Downstream service returned an error.");
        }

        return (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.");
    }

    private static bool ExceptionChainContainsOperationCanceled(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is OperationCanceledException)
            {
                return true;
            }
        }

        return false;
    }
}

