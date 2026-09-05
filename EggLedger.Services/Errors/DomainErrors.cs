using FluentResults;

namespace EggLedger.Services.Errors;

/// <summary>Requested resource doesn't exist or isn't visible to the caller. Maps to HTTP 404.</summary>
public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message)
    {
    }
}

/// <summary>Caller is authenticated but not allowed to perform this action. Maps to HTTP 403.</summary>
public class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message)
    {
    }
}

/// <summary>Request conflicts with the resource's current state. Maps to HTTP 409.</summary>
public class ConflictError : Error
{
    public ConflictError(string message) : base(message)
    {
    }
}

/// <summary>
/// A genuinely unexpected failure (an exception was caught), as opposed to an expected
/// business outcome. Maps to HTTP 500.
/// </summary>
public class UnexpectedError : Error
{
    public UnexpectedError(string message) : base(message)
    {
    }
}
