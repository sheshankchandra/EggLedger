using System.Security.Claims;
using EggLedger.API.Extensions;
using EggLedger.DTO.Ledger;
using EggLedger.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Controllers;

// All endpoints require the caller to be a member of the room named in the route - see
// RoomMemberHandler, which reads {roomCode} straight off the route data.
[ApiController]
[Route("egg-ledger-api/room/{roomCode:int}/ledger")]
[Authorize(Policy = "RoomMember")]
public class LedgerController : ControllerBase
{
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<LedgerController> _logger;

    public LedgerController(ILedgerService ledgerService, ILogger<LedgerController> logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    // GET: egg-ledger-api/room/{roomCode}/ledger
    [HttpGet]
    public async Task<IActionResult> GetLedger([FromRoute] int roomCode, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _ledgerService.GetRoomLedgerAsync(roomCode, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetLedger, roomCode: {RoomCode}", roomCode);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetLedger for roomCode: {RoomCode}", roomCode);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // GET: egg-ledger-api/room/{roomCode}/ledger/history
    [HttpGet("history")]
    public async Task<IActionResult> GetSettlementHistory([FromRoute] int roomCode, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _ledgerService.GetSettlementHistoryAsync(roomCode, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetSettlementHistory, roomCode: {RoomCode}", roomCode);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetSettlementHistory for roomCode: {RoomCode}", roomCode);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // POST: egg-ledger-api/room/{roomCode}/ledger/settle
    // The caller is always the Receiver - only the person who received money can confirm it.
    [HttpPost("settle")]
    public async Task<IActionResult> RecordSettlement([FromRoute] int roomCode, [FromBody] SettlementCreateDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var receiverId))
        {
            return Problem(detail: "Invalid user identity", statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
        }

        try
        {
            var result = await _ledgerService.RecordSettlementAsync(receiverId, roomCode, dto, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Settlement recorded in room {RoomCode} by {ReceiverId}", roomCode, receiverId);
                return Ok(result.Value);
            }

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for RecordSettlement, roomCode: {RoomCode}", roomCode);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in RecordSettlement for roomCode: {RoomCode}", roomCode);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // DELETE: egg-ledger-api/room/{roomCode}/ledger/settle/{settlementId}
    [HttpDelete("settle/{settlementId:guid}")]
    public async Task<IActionResult> DeleteSettlement([FromRoute] int roomCode, [FromRoute] Guid settlementId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var callerId))
        {
            return Problem(detail: "Invalid user identity", statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
        }

        try
        {
            var result = await _ledgerService.DeleteSettlementAsync(roomCode, settlementId, callerId, cancellationToken);
            if (result.IsSuccess)
                return Ok();
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for DeleteSettlement, roomCode: {RoomCode}", roomCode);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in DeleteSettlement for roomCode: {RoomCode}", roomCode);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }
}
