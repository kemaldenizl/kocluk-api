using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Tenant.API.Common.ErrorMapping;

/// <summary>
/// Turns a FluentValidation <see cref="ValidationException"/> thrown by the MediatR validation behavior
/// into a 400 validation problem response, keyed by property name, instead of a 500.
/// </summary>
public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        var errors = validationException.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        var problem = Results.ValidationProblem(
            errors,
            title: "One or more validation errors occurred.",
            statusCode: StatusCodes.Status400BadRequest);

        await problem.ExecuteAsync(httpContext);
        return true;
    }
}
