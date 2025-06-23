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
    }
}