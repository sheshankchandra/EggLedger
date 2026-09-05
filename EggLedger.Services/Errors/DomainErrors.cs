using FluentResults;

namespace EggLedger.Services.Errors;

/// <summary>
/// The requested resource does not exist (or isn't visible to the caller). Controllers map
/// this to HTTP 404, replacing fragile string comparisons like
/// <c>e.Message == "Room not found"</c> with a type check that survives message wording changes.
/// </summary>
public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message)
    {
    }
}

/// <summary>
/// The caller is authenticated but not allowed to perform this action. Controllers map this
/// to HTTP 403.
/// </summary>
public class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message)
    {
    }
}

/// <summary>
/// The request conflicts with the resource's current state (e.g. it was already actioned).
/// Controllers map this to HTTP 409.
/// </summary>
public class ConflictError : Error
{
    public ConflictError(string message) : base(message)
    {
    }
}
