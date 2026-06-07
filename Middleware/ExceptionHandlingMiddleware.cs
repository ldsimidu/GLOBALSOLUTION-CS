using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GlobalSolution.SenseSpot.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database update failed while processing {Method} {Path}.", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "Database operation failed.", "The API is online, but the database operation could not be completed.");
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Invalid format while processing {Method} {Path}.", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Invalid data format.", "Check the request values and try again.");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while processing {Method} {Path}.", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Invalid operation.", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected failure while processing {Method} {Path}.", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Unexpected error.", "The API remained available, but this request could not be completed.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, problem);
    }
}
