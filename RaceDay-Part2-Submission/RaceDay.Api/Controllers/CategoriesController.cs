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
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
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
        /// Updates a category's details (Organiser owner only).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        [SwaggerOperation(Summary = "Updates category details", Description = "Organiser owner only.")]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Category>> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
        {
            var userId = GetCurrentUserId();
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { error = $"Category with ID {id} was not found." });
            }

            if (category.Event?.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You are not the owner of this event category." });
            }

            if (!string.IsNullOrWhiteSpace(dto.CategoryName)) category.CategoryName = dto.CategoryName.Trim();
            if (dto.DistanceKm.HasValue) category.DistanceKm = dto.DistanceKm.Value;
            if (dto.MinAge.HasValue) category.MinAge = dto.MinAge.Value;
            if (dto.MaxAge.HasValue) category.MaxAge = dto.MaxAge.Value;
            if (dto.EntryFee.HasValue) category.EntryFee = dto.EntryFee.Value;
            if (dto.MaxParticipants.HasValue) category.MaxParticipants = dto.MaxParticipants.Value;

            await _context.SaveChangesAsync();
            return Ok(category);
        }

        /// <summary>
        /// Removes a category from an event (Organiser owner only).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        [SwaggerOperation(Summary = "Deletes a race category", Description = "Organiser owner only.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = GetCurrentUserId();
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { error = $"Category with ID {id} was not found." });
            }

            if (category.Event?.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You are not the owner of this event category." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Enrols the logged-in participant into a category, generating a race number.
        /// </summary>
        [HttpPost("{id}/enrol")]
        [Authorize(Roles = "Participant")]
        [SwaggerOperation(Summary = "Enrols participant into category", Description = "Participant only. Enforces no duplicate enrolments and category participant limits.")]
        [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EnrolmentResponseDto>> Enrol(int id, [FromBody] EnrolDto? dto)
        {
            var participantId = GetCurrentUserId();
            if (!participantId.HasValue)
            {
                return Unauthorized(new { error = "User is not authenticated." });
            }

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { error = $"Category with ID {id} was not found." });
            }

            // Check duplicate enrolment (Requirement: one participant cannot enrol in same category twice)
            var existingEnrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.ParticipantId == participantId.Value && e.CategoryId == id);

            if (existingEnrolment != null)
            {
                return Conflict(new { error = "Conflict: Participant is already enrolled in this category." });
            }

            // Check participant cap
            if (category.MaxParticipants.HasValue)
            {
                var currentCount = await _context.Enrolments.CountAsync(e => e.CategoryId == id);
                if (currentCount >= category.MaxParticipants.Value)
                {
                    return BadRequest(new { error = "Bad Request: Category is full to maximum capacity." });
                }
            }

            // Generate Race Number (Bib)
            var prefix = category.DistanceKm >= 42 ? "A" : category.DistanceKm >= 21 ? "B" : category.DistanceKm >= 10 ? "C" : "W";
            var randomSuffix = Random.Shared.Next(1000, 9999);
            var raceNumber = $"{prefix}{randomSuffix}";

            var participant = await _context.Users.FindAsync(participantId.Value);

            var enrolment = new Enrolment
            {
                ParticipantId = participantId.Value,
                CategoryId = id,
                RaceNumber = raceNumber,
                EnrolmentDate = DateTime.UtcNow,
                PaymentStatus = category.EntryFee == 0 ? "Paid" : (dto?.PaymentStatus ?? "Paid")
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            var responseDto = new EnrolmentResponseDto
            {
                EnrolmentId = enrolment.EnrolmentId,
                ParticipantId = enrolment.ParticipantId,
                CategoryId = enrolment.CategoryId,
                RaceNumber = enrolment.RaceNumber,
                EnrolmentDate = enrolment.EnrolmentDate,
                PaymentStatus = enrolment.PaymentStatus,
                ParticipantName = participant?.FullName,
                CategoryName = category.CategoryName,
                EventName = category.Event?.EventName,
                EntryFee = category.EntryFee,
                DistanceKm = category.DistanceKm,
                HasResult = false
            };

            return CreatedAtAction("GetMyEnrolments", "Users", null, responseDto);
        }

        /// <summary>
        /// Returns the results/leaderboard for a category, sorted by finish position.
        /// </summary>
        [HttpGet("{id}/results")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Gets category leaderboard", Description = "Public endpoint. Returns finish results sorted by overall finish position.")]
        [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ResultResponseDto>>> GetCategoryResults(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { error = $"Category with ID {id} was not found." });
            }

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Participant)
                .Where(r => r.Enrolment!.CategoryId == id)
                .OrderBy(r => r.OverallPosition)
                .AsNoTracking()
                .ToListAsync();

            var dtos = results.Select(r => new ResultResponseDto
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                FinishTime = r.FinishTime?.ToString(@"hh\:mm\:ss"),
                OverallPosition = r.OverallPosition,
                CategoryPosition = r.CategoryPosition,
                Status = r.Status,
                RaceNumber = r.Enrolment?.RaceNumber,
                ParticipantName = r.Enrolment?.Participant?.FullName,
                CategoryName = category.CategoryName,
                DistanceKm = category.DistanceKm,
                EventName = category.Event?.EventName,
                EventDate = category.Event?.EventDate,
                Location = category.Event?.Location
            });

            return Ok(dtos);
        }
    }
}
