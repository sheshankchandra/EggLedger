using EggLedger.Services.Errors;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Extensions;

/// <summary>
/// Maps a failed <see cref="IResultBase"/> to an RFC 7807 ProblemDetails response, so every
/// controller's error responses share one shape instead of each inventing its own.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Only call this when <c>result.IsSuccess</c> is false.</summary>
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
