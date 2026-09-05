using System.Security.Claims;
using EggLedger.API.Extensions;
using EggLedger.DTO.Container;
using EggLedger.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Controllers;

[ApiController]
[Route("egg-ledger-api/room/{roomCode:int}/container")]
public class ContainerController : ControllerBase
{
    private readonly IContainerService _containerService;
    private readonly ILogger<ContainerController> _logger;

    public ContainerController(IContainerService containerService, ILogger<ContainerController> logger)
    {
        _containerService = containerService;
        _logger = logger;
    }

    // GET: egg-ledger-api/room/{roomCode}/container/all
    [Authorize(Policy = "RoomMember")]
    [HttpGet("all")]
    public async Task<ActionResult<List<ContainerSummaryDto>>> GetAllContainers([FromRoute] int roomCode, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _containerService.GetAllContainersAsync(roomCode, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetAllContainers, roomCode: {RoomCode}", roomCode);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetAllContainers for roomCode: {RoomCode}", roomCode);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // GET: egg-ledger-api/room/{roomCode}/container/{id}
    [Authorize(Policy = "RoomMember")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ContainerSummaryDto>> GetContainer([FromRoute] int roomCode, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _containerService.GetContainerAsync(id, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetContainer, roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetContainer for roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // POST: egg-ledger-api/room/{roomCode}/container/create
    [Authorize(Policy = "RoomMember")]
    [HttpPost("create")]
    public async Task<IActionResult> CreateContainer([FromRoute] int roomCode, [FromBody] ContainerCreateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _containerService.CreateContainerAsync(roomCode, dto, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for CreateContainer, roomCode: {RoomCode}", roomCode);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in CreateContainer for roomCode: {RoomCode}", roomCode);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // PUT: egg-ledger-api/room/{roomCode}/container/update/{id}
    [Authorize(Policy = "RoomMember")]
    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateContainer([FromRoute] int roomCode, Guid id, [FromBody] ContainerUpdateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _containerService.UpdateContainerAsync(id, dto, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for UpdateContainer, roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in UpdateContainer for roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // PUT: egg-ledger-api/room/{roomCode}/container/archive/{id}
    [Authorize(Policy = "RoomMember")]
    [HttpPut("archive/{id}")]
    public async Task<IActionResult> ArchiveContainer([FromRoute] int roomCode, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _containerService.ArchiveContainerAsync(id, cancellationToken);
            if (result.IsSuccess)
                return NoContent();

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for ArchiveContainer, roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in ArchiveContainer for roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // DELETE: egg-ledger-api/room/{roomCode}/container/delete/{id}
    [Authorize(Policy = "RoomMember")]
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteContainer([FromRoute] int roomCode, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
            var result = await _containerService.DeleteContainerAsync(id, userId, cancellationToken);
            if (result.IsSuccess)
                return NoContent();

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for DeleteContainer, roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in DeleteContainer for roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // PUT: egg-ledger-api/room/{roomCode}/container/suspend/{id}
    [Authorize(Policy = "RoomMember")]
    [HttpPut("suspend/{id}")]
    public async Task<IActionResult> SuspendContainer([FromRoute] int roomCode, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _containerService.SuspendContainerAsync(id, cancellationToken);
            if (result.IsSuccess)
                return NoContent();

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for SuspendContainer, roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in SuspendContainer for roomCode: {RoomCode}, id: {Id}", roomCode, id);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // GET: egg-ledger-api/room/{roomCode}/container/user/{name}
    [Authorize(Policy = "RoomMember")]
    [HttpGet("user/{name}")]
    public async Task<ActionResult<List<ContainerSummaryDto>>> SearchContainers([FromRoute] int roomCode, [FromRoute] string name, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _containerService.SearchContainersByOwnerNameAsync(roomCode, name, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for SearchContainers, roomCode: {RoomCode}, name: {Name}", roomCode, name);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in SearchContainers for roomCode: {RoomCode}, name: {Name}", roomCode, name);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // GET: egg-ledger-api/room/{roomCode}/container/user/all
    [Authorize(Policy = "RoomMember")]
    [HttpGet("user/all")]
    public async Task<ActionResult<List<ContainerSummaryDto>>> GetMyContainers([FromRoute] int roomCode, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received request to GetMyContainers");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Problem(detail: "Invalid user identity", statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
            }

            var result = await _containerService.GetMyContainers(userId, roomCode, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetMyContainers, roomCode: {RoomCode}", roomCode);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetMyContainers for roomCode: {RoomCode}", roomCode);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // GET: egg-ledger-api/room/{roomCode}/container/paged?page=1&pageSize=20
    [Authorize(Policy = "RoomMember")]
    [HttpGet("paged")]
    public async Task<ActionResult<List<ContainerSummaryDto>>> GetPagedContainers([FromRoute] int roomCode, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _containerService.GetPagedContainersAsync(roomCode, page, pageSize, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);

            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetPagedContainers, roomCode: {RoomCode}, page: {Page}, pageSize: {PageSize}", roomCode, page, pageSize);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetPagedContainers for roomCode: {RoomCode}, page: {Page}, pageSize: {PageSize}", roomCode, page, pageSize);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }
}
