using System.Runtime.CompilerServices;
using EggLedger.Services.Errors;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Extensions;

/// <summary>
/// Wraps a service operation with the try/catch/log/Result pattern every service method in this
/// layer previously repeated by hand: cancellation becomes a logged Info + a "canceled" failure,
/// any other exception becomes a logged Error + the caller-supplied failure message.
/// </summary>
public static class ServiceResultExtensions
{
    public static async Task<Result<T>> ExecuteAsync<T>(
        this ILogger logger,
        Func<Task<Result<T>>> operation,
        string failureMessage,
        [CallerMemberName] string operationName = "")
    {
        try
        {
            return await operation();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogInformation(ex, "{Operation} was canceled", operationName);
            return Result.Fail("Operation was canceled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in {Operation}", operationName);
            return Result.Fail(new UnexpectedError(failureMessage));
        }
    }

    public static async Task<Result> ExecuteAsync(
        this ILogger logger,
        Func<Task<Result>> operation,
        string failureMessage,
        [CallerMemberName] string operationName = "")
    {
        try
        {
            return await operation();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogInformation(ex, "{Operation} was canceled", operationName);
            return Result.Fail("Operation was canceled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in {Operation}", operationName);
            return Result.Fail(new UnexpectedError(failureMessage));
        }
    }
}
