using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RolesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public RolesController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lists the roles a user can register as (Organiser, Participant).
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Lists all available user roles", Description = "Public lookup endpoint. Returns Organiser (role_id 1) and Participant (role_id 2).")]
        [ProducesResponseType(typeof(IEnumerable<Role>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            var roles = await _context.Roles.AsNoTracking().ToListAsync();
            return Ok(roles);
        }
    }
}
