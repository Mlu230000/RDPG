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
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
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
        /// Returns details of an enrolment by ID.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Gets enrolment by ID", Description = "Participant owner or event organiser.")]
        [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EnrolmentResponseDto>> GetEnrolmentById(int id)
        {
            var userId = GetCurrentUserId();
            var e = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .Include(e => e.Result)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (e == null)
            {
                return NotFound(new { error = $"Enrolment with ID {id} was not found." });
            }

            var isOwner = e.ParticipantId == userId;
            var isOrganiser = e.Category?.Event?.OrganiserId == userId;

            if (!isOwner && !isOrganiser)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You are not authorized to view this enrolment." });
            }

            return Ok(new EnrolmentResponseDto
            {
                EnrolmentId = e.EnrolmentId,
                ParticipantId = e.ParticipantId,
                CategoryId = e.CategoryId,
                RaceNumber = e.RaceNumber,
                EnrolmentDate = e.EnrolmentDate,
                PaymentStatus = e.PaymentStatus,
                ParticipantName = e.Participant?.FullName,
                CategoryName = e.Category?.CategoryName,
                EventName = e.Category?.Event?.EventName,
                EntryFee = e.Category?.EntryFee,
                DistanceKm = e.Category?.DistanceKm,
                HasResult = e.Result != null
            });
        }

        /// <summary>
        /// Cancels/withdraws a participant's own enrolment (Participant owner only).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Participant")]
        [SwaggerOperation(Summary = "Withdraws participant enrolment", Description = "Participant owner only. Removes enrolment and any associated result.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var userId = GetCurrentUserId();
            var enrolment = await _context.Enrolments
                .Include(e => e.Result)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new { error = $"Enrolment with ID {id} was not found." });
            }

            if (enrolment.ParticipantId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You may only cancel your own enrolment." });
            }

            if (enrolment.Result != null)
            {
                _context.Results.Remove(enrolment.Result);
            }

            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Captures a finish-time result against a participant's enrolment (Organiser owner only).
        /// </summary>
        [HttpPost("{id}/results")]
        [Authorize(Roles = "Organiser")]
        [SwaggerOperation(Summary = "Captures race result for enrolment", Description = "Organiser owner only. Captures finish time, positions, and status (Finished, DNF, DSQ).")]
        [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultResponseDto>> CaptureResult(int id, [FromBody] CaptureResultDto dto)
        {
            var userId = GetCurrentUserId();
            var enrolment = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .Include(e => e.Result)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new { error = $"Enrolment with ID {id} was not found." });
            }

            if (enrolment.Category?.Event?.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You are not the organiser of this event." });
            }

            if (enrolment.Result != null)
            {
                return Conflict(new { error = "Conflict: A result has already been captured for this enrolment. Use PUT /api/results/{id} to update." });
            }

            if (!TimeSpan.TryParse(dto.FinishTime, out var finishTimeSpan))
            {
                return BadRequest(new { error = "Invalid finishTime format. Expected HH:mm:ss (e.g. 01:45:30)." });
            }

            var validStatuses = new[] { "Finished", "DNF", "DSQ" };
            var status = string.IsNullOrWhiteSpace(dto.Status) ? "Finished" : dto.Status.Trim();
            if (!validStatuses.Contains(status))
            {
                return BadRequest(new { error = "Status must be 'Finished', 'DNF', or 'DSQ'." });
            }

            var totalCategoryResults = await _context.Results
                .CountAsync(r => r.Enrolment!.CategoryId == enrolment.CategoryId);

            var result = new Result
            {
                EnrolmentId = id,
                FinishTime = finishTimeSpan,
                OverallPosition = dto.OverallPosition ?? (totalCategoryResults + 1),
                CategoryPosition = dto.CategoryPosition ?? (totalCategoryResults + 1),
                Status = status
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var responseDto = new ResultResponseDto
            {
                ResultId = result.ResultId,
                EnrolmentId = result.EnrolmentId,
                FinishTime = result.FinishTime?.ToString(@"hh\:mm\:ss"),
                OverallPosition = result.OverallPosition,
                CategoryPosition = result.CategoryPosition,
                Status = result.Status,
                RaceNumber = enrolment.RaceNumber,
                ParticipantName = enrolment.Participant?.FullName,
                CategoryName = enrolment.Category?.CategoryName,
                DistanceKm = enrolment.Category?.DistanceKm,
                EventName = enrolment.Category?.Event?.EventName,
                EventDate = enrolment.Category?.Event?.EventDate,
                Location = enrolment.Category?.Event?.Location
            };

            return CreatedAtAction(nameof(ResultsController.GetResultById), "Results", new { id = result.ResultId }, responseDto);
        }
    }
}
