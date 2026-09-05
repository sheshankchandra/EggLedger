using EggLedger.Services.Errors;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Extensions;

/// <summary>
/// Single place that turns a failed <see cref="IResultBase"/> into an HTTP response. Every
/// controller failure path goes through <see cref="ToProblem"/> so every error response -
/// regardless of which service or entity it came from - is the same RFC 7807 ProblemDetails
/// envelope, instead of each controller inventing its own status code and body shape.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a failed result's error type to a status code and returns a ProblemDetails result.
    /// Only call this when <c>result.IsSuccess</c> is false.
    /// </summary>
    public static ActionResult ToProblem(this ControllerBase controller, IResultBase result)
    {
        var statusCode = result.Errors.Any(e => e is NotFoundError) ? StatusCodes.Status404NotFound
            : result.Errors.Any(e => e is ForbiddenError) ? StatusCodes.Status403Forbidden
            : result.Errors.Any(e => e is ConflictError) ? StatusCodes.Status409Conflict
            : result.Errors.Any(e => e is UnexpectedError) ? StatusCodes.Status500InternalServerError
            : StatusCodes.Status400BadRequest;

        var detail = string.Join(" ", result.Errors.Select(e => e.Message));

        return controller.Problem(detail: detail, statusCode: statusCode);
    }
}
