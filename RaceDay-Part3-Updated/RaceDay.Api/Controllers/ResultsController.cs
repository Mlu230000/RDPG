using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var userId)) return userId;

            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue) return sessionUserId.Value;

            return null;
        }

        /// <summary>
        /// Returns detail for a specific race result by ID.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Gets result by ID", Description = "Public endpoint. Returns result details with participant and event metadata.")]
        [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultResponseDto>> GetResultById(int id)
        {
            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Participant)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                        .ThenInclude(c => c!.Event)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound(new { error = $"Result with ID {id} was not found." });
            }

            return Ok(new ResultResponseDto
            {
                ResultId = result.ResultId,
                EnrolmentId = result.EnrolmentId,
                FinishTime = result.FinishTime?.ToString(@"hh\:mm\:ss"),
                OverallPosition = result.OverallPosition,
                CategoryPosition = result.CategoryPosition,
                Status = result.Status,
                RaceNumber = result.Enrolment?.RaceNumber,
                ParticipantName = result.Enrolment?.Participant?.FullName,
                CategoryName = result.Enrolment?.Category?.CategoryName,
                DistanceKm = result.Enrolment?.Category?.DistanceKm,
                EventName = result.Enrolment?.Category?.Event?.EventName,
                EventDate = result.Enrolment?.Category?.Event?.EventDate,
                Location = result.Enrolment?.Category?.Event?.Location
            });
        }

        /// <summary>
        /// Corrects an already-captured result (Organiser owner only).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        [SwaggerOperation(Summary = "Updates an existing race result", Description = "Organiser owner only. Allows correcting finish time, overall/category position, or status.")]
        [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultResponseDto>> UpdateResult(int id, [FromBody] UpdateResultDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Participant)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                        .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound(new { error = $"Result with ID {id} was not found." });
            }

            if (result.Enrolment?.Category?.Event?.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You are not the organiser of this event." });
            }

            if (!string.IsNullOrWhiteSpace(dto.FinishTime))
            {
                if (TimeSpan.TryParse(dto.FinishTime, out var t))
                {
                    result.FinishTime = t;
                }
                else
                {
                    return BadRequest(new { error = "Invalid finishTime format. Expected HH:mm:ss." });
                }
            }

            if (dto.OverallPosition.HasValue)
            {
                result.OverallPosition = dto.OverallPosition.Value;
            }

            if (dto.CategoryPosition.HasValue)
            {
                result.CategoryPosition = dto.CategoryPosition.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                var validStatuses = new[] { "Finished", "DNF", "DSQ" };
                if (!validStatuses.Contains(dto.Status.Trim()))
                {
                    return BadRequest(new { error = "Status must be 'Finished', 'DNF', or 'DSQ'." });
                }
                result.Status = dto.Status.Trim();
            }

            await _context.SaveChangesAsync();

            var responseDto = new ResultResponseDto
            {
                ResultId = result.ResultId,
                EnrolmentId = result.EnrolmentId,
                FinishTime = result.FinishTime?.ToString(@"hh\:mm\:ss"),
                OverallPosition = result.OverallPosition,
                CategoryPosition = result.CategoryPosition,
                Status = result.Status,
                RaceNumber = result.Enrolment?.RaceNumber,
                ParticipantName = result.Enrolment?.Participant?.FullName,
                CategoryName = result.Enrolment?.Category?.CategoryName,
                DistanceKm = result.Enrolment?.Category?.DistanceKm,
                EventName = result.Enrolment?.Category?.Event?.EventName,
                EventDate = result.Enrolment?.Category?.Event?.EventDate,
                Location = result.Enrolment?.Category?.Event?.Location
            };

            return Ok(responseDto);
        }
    }
}
