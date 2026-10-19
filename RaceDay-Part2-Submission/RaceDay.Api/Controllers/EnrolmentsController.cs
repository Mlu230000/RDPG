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

            if (Request.Headers.TryGetValue("x-user-id", out var directHeader) &&
                int.TryParse(directHeader, out var parsedId))
            {
                return parsedId;
            }

            return null;
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

            TimeSpan finishTimeSpan;
            if (!TimeSpan.TryParse(dto.FinishTime, out finishTimeSpan))
            {
                return BadRequest(new { error = "Invalid finishTime format. Expected HH:mm:ss (e.g. 01:45:30)." });
            }

            var totalCategoryResults = await _context.Results
                .CountAsync(r => r.Enrolment!.CategoryId == enrolment.CategoryId);

            var result = new Result
            {
                EnrolmentId = id,
                FinishTime = finishTimeSpan,
                OverallPosition = dto.OverallPosition ?? (totalCategoryResults + 1),
                CategoryPosition = dto.CategoryPosition ?? (totalCategoryResults + 1),
                Status = dto.Status ?? "Finished"
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

            return CreatedAtAction("GetCategoryResults", "Categories", new { id = enrolment.CategoryId }, responseDto);
        }
    }
}
