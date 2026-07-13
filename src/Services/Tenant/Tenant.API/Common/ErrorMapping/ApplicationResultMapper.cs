using Tenant.Application.Common.Errors;
using Tenant.Application.Common.Results;

namespace Tenant.API.Common.ErrorMapping;

/// <summary>
/// Translates an application <see cref="Result"/> failure into an RFC 7807 problem response, mapping the
/// stable error code to the right HTTP status. Success cases are shaped by the individual endpoints.
/// </summary>
public static class ApplicationResultMapper
{
    public static IResult ToApiResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : MapFailure(result.Error);

    public static IResult MapFailure(Error error) => error.Code switch
    {
        "tenant.not_found" => Results.Problem(
            title: "Tenant not found",
            detail: error.Description,
            statusCode: StatusCodes.Status404NotFound),

        "tenant.slug_already_exists" => Results.Problem(
            title: "Conflict",
            detail: error.Description,
            statusCode: StatusCodes.Status409Conflict),

        _ => Results.Problem(
            title: "Application error",
            detail: error.Description,
            statusCode: StatusCodes.Status400BadRequest)
    };
}
