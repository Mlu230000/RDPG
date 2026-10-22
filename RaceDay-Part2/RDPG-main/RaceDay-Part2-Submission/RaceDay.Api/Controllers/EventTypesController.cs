using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/event-types")]
    [Produces("application/json")]
    public class EventTypesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventTypesController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lists the event types (Running, Cycling, Walking) used to categorise events.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Lists all event types", Description = "Public lookup endpoint. Returns Running (1), Cycling (2), and Walking (3).")]
        [ProducesResponseType(typeof(IEnumerable<EventType>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EventType>>> GetEventTypes()
        {
            var eventTypes = await _context.EventTypes.AsNoTracking().ToListAsync();
            return Ok(eventTypes);
        }
    }
}
