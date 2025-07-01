using EggLedger.Core.DTOs.Container;
using EggLedger.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        // GET: api/room/{roomCode}/container/all
        [Authorize(Policy = "RoomMember")]
        [HttpGet("all")]
        public async Task<ActionResult<List<ContainerSummaryDto>>> GetAllContainers([FromRoute] int roomCode)
        {
            var result = await _containerService.GetAllContainersAsync(roomCode);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors);
        }

        // GET: api/room/{roomCode}/container/{id}
        [Authorize(Policy = "RoomMember")]
        [HttpGet("{id}")]
        public async Task<ActionResult<ContainerSummaryDto>> GetContainer([FromRoute] int roomCode, Guid id)
        {
            var result = await _containerService.GetContainerAsync(id);
            if (result.IsSuccess)
                return Ok(result.Value);

            if (result.Errors.Any(e => e.Message == "Container not Found"))
                return NotFound();

            return StatusCode(500, result.Errors);
        }

        // POST: api/room/{roomCode}/container/create
        [Authorize(Policy = "RoomMember")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateContainer([FromRoute] int roomCode, [FromBody] ContainerCreateDto dto)
        {
            var result = await _containerService.CreateContainerAsync(roomCode, dto);
            if (result.IsSuccess)
                return Ok(result.Value);

            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // PUT: api/room/{roomCode}/container/{id}
        [Authorize(Policy = "RoomMember")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateContainer([FromRoute] int roomCode, Guid id, [FromBody] ContainerUpdateDto dto)
        {
            var result = await _containerService.UpdateContainerAsync(id, dto);
            if (result.IsSuccess)
                return Ok(result.Value);

            if (result.Errors.Any(e => e.Message == "Container not Found"))
                return NotFound();

            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // DELETE: api/room/{roomCode}/container/{id}
        [Authorize(Policy = "RoomMember")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteContainer([FromRoute] int roomCode, Guid id)
        {
            var result = await _containerService.DeleteContainerAsync(id);
            if (result.IsSuccess)
                return NoContent();

            if (result.Errors.Any(e => e.Message == "Container not Found"))
                return NotFound();

            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // GET: api/room/{roomCode}/container/user/{name}
        [Authorize(Policy = "RoomMember")]
        [HttpGet("user/{name}")]
        public async Task<ActionResult<List<ContainerSummaryDto>>> SearchContainers([FromRoute] int roomCode, [FromRoute] string name)
        {
            var result = await _containerService.SearchContainersByOwnerNameAsync(roomCode, name);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors);
        }

        // GET: api/room/{roomCode}/container/paged?page=1&pageSize=20
        [Authorize(Policy = "RoomMember")]
        [HttpGet("paged")]
        public async Task<ActionResult<List<ContainerSummaryDto>>> GetPagedContainers([FromRoute] int roomCode, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _containerService.GetPagedContainersAsync(roomCode, page, pageSize);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors);
        }
    }
}
