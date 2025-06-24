using EggLedger.Core.DTOs.Container;
using EggLedger.Core.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContainerController : ControllerBase
    {
        private readonly IContainerService _containerService;

        public ContainerController(IContainerService containerService)
        {
            _containerService = containerService;
        }

        // GET: api/container
        [HttpGet]
        public async Task<ActionResult<List<ContainerSummaryDto>>> GetAllContainers()
        {
            var result = await _containerService.GetAllContainersAsync();
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors);
        }

        // GET: api/container/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ContainerSummaryDto>> GetContainer(Guid id)
        {
            var result = await _containerService.GetContainerAsync(id);
            if (result.IsSuccess)
                return Ok(result.Value);

            if (result.Errors.Any(e => e.Message == "Container not Found"))
                return NotFound();

            return StatusCode(500, result.Errors);
        }

        // POST: api/container
        [HttpPost]
        public async Task<IActionResult> CreateContainer([FromBody] ContainerCreateDto dto)
        {
            var result = await _containerService.CreateContainerAsync(dto);
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetContainer), new { id = result.Value.ContainerId }, result.Value);

            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // PUT: api/container/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContainer(Guid id, [FromBody] ContainerUpdateDto dto)
        {
            var result = await _containerService.UpdateContainerAsync(id, dto);
            if (result.IsSuccess)
                return Ok(result.Value);

            if (result.Errors.Any(e => e.Message == "Container not Found"))
                return NotFound();

            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // DELETE: api/container/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContainer(Guid id)
        {
            var result = await _containerService.DeleteContainerAsync(id);
            if (result.IsSuccess)
                return NoContent();

            if (result.Errors.Any(e => e.Message == "Container not Found"))
                return NotFound();

            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // GET: api/container/search?ownerName=John
        [HttpGet("search")]
        public async Task<ActionResult<List<ContainerSummaryDto>>> SearchContainers([FromQuery] string ownerName)
        {
            var result = await _containerService.SearchContainersByOwnerNameAsync(ownerName);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors);
        }

        // GET: api/container/paged?page=1&pageSize=20
        [HttpGet("paged")]
        public async Task<ActionResult<List<ContainerSummaryDto>>> GetPagedContainers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _containerService.GetPagedContainersAsync(page, pageSize);
            if (result.IsSuccess)
                return Ok(result.Value);

            return StatusCode(500, result.Errors);
        }
    }
}
