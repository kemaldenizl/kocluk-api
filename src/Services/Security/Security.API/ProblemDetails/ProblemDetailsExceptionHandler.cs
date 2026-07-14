using Microsoft.AspNetCore.Diagnostics;

namespace Security.API.ProblemDetails;

public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            BadHttpRequestException badRequest => (
                badRequest.StatusCode,
                "Invalid request",
                "The request body could not be read. Please check the format and field values."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.")
        };

        var problem = httpContext.CreateProblemDetails(statusCode, title, detail);

        await Results.Problem(problem).ExecuteAsync(httpContext);

        return true;
    }
}
