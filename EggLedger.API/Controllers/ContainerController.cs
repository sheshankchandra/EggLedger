using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EggLedger.DTO.Container;
using EggLedger.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EggLedger.API.Controllers
{
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

                return StatusCode(500, result.Errors);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for GetAllContainers, roomCode: {RoomCode}", roomCode);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetAllContainers for roomCode: {RoomCode}", roomCode);
                return StatusCode(500, "An unexpected error occurred.");
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

                if (result.Errors.Any(e => e.Message == "Container not found"))
                    return NotFound();

                return StatusCode(500, result.Errors);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for GetContainer, roomCode: {RoomCode}, id: {Id}", roomCode, id);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetContainer for roomCode: {RoomCode}, id: {Id}", roomCode, id);
                return StatusCode(500, "An unexpected error occurred.");
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

                return BadRequest(result.Errors.Select(e => e.Message));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for CreateContainer, roomCode: {RoomCode}", roomCode);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateContainer for roomCode: {RoomCode}", roomCode);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // PUT: egg-ledger-api/room/{roomCode}/container/{id}
        [Authorize(Policy = "RoomMember")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateContainer([FromRoute] int roomCode, Guid id, [FromBody] ContainerUpdateDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _containerService.UpdateContainerAsync(id, dto, cancellationToken);
                if (result.IsSuccess)
                    return Ok(result.Value);

                if (result.Errors.Any(e => e.Message == "Container not found"))
                    return NotFound();

                return BadRequest(result.Errors.Select(e => e.Message));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for UpdateContainer, roomCode: {RoomCode}, id: {Id}", roomCode, id);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in UpdateContainer for roomCode: {RoomCode}, id: {Id}", roomCode, id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // DELETE: egg-ledger-api/room/{roomCode}/container/{id}
        [Authorize(Policy = "RoomMember")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteContainer([FromRoute] int roomCode, Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _containerService.DeleteContainerAsync(id, cancellationToken);
                if (result.IsSuccess)
                    return NoContent();

                if (result.Errors.Any(e => e.Message == "Container not found"))
                    return NotFound();

                return BadRequest(result.Errors.Select(e => e.Message));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for DeleteContainer, roomCode: {RoomCode}, id: {Id}", roomCode, id);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in DeleteContainer for roomCode: {RoomCode}, id: {Id}", roomCode, id);
                return StatusCode(500, "An unexpected error occurred.");
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

                return StatusCode(500, result.Errors);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for SearchContainers, roomCode: {RoomCode}, name: {Name}", roomCode, name);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in SearchContainers for roomCode: {RoomCode}, name: {Name}", roomCode, name);
                return StatusCode(500, "An unexpected error occurred.");
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

                return StatusCode(500, result.Errors);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was canceled by the client for GetPagedContainers, roomCode: {RoomCode}, page: {Page}, pageSize: {PageSize}", roomCode, page, pageSize);
                return StatusCode(499, "Client closed request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetPagedContainers for roomCode: {RoomCode}, page: {Page}, pageSize: {PageSize}", roomCode, page, pageSize);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}