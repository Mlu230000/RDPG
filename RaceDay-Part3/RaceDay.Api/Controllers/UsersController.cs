using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var userId))
            {
                return userId;
            }

            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue)
            {
                return sessionUserId.Value;
            }

            return null;
        }

        /// <summary>
        /// Returns the profile of the currently logged-in user, including their role.
        /// </summary>
        [HttpGet("me")]
        [SwaggerOperation(Summary = "Gets current user profile", Description = "Requires an authenticated user session or token.")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> GetMe()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User is not authenticated." });
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return NotFound(new { error = "User profile not found." });
            }

            return Ok(new UserDto
            {
                UserId = user.UserId,
                RoleId = user.RoleId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role?.RoleName ?? "Participant",
                CreatedAt = user.CreatedAt
            });
        }

        /// <summary>
        /// Updates the logged-in user's own profile details.
        /// </summary>
        [HttpPut("me")]
        [SwaggerOperation(Summary = "Updates current user profile", Description = "Allows updating fullName and phone number.")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User is not authenticated." });
            }

            if (string.IsNullOrWhiteSpace(dto.FullName) && dto.Phone == null)
            {
                return BadRequest(new { error = "At least FullName or Phone must be provided for update." });
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return NotFound(new { error = "User profile not found." });
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                user.FullName = dto.FullName.Trim();
            }

            if (dto.Phone != null)
            {
                user.Phone = dto.Phone.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new UserDto
            {
                UserId = user.UserId,
                RoleId = user.RoleId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role?.RoleName ?? "Participant",
                CreatedAt = user.CreatedAt
            });
        }

        /// <summary>
        /// Lists the logged-in participant's own event enrolments.
        /// </summary>
        [HttpGet("me/enrolments")]
        [Authorize(Roles = "Participant")]
        [SwaggerOperation(Summary = "Lists participant enrolments", Description = "Participant only. Returns all event enrolments with category info.")]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<EnrolmentResponseDto>>> GetMyEnrolments()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User is not authenticated." });
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantId == userId.Value)
                .Include(e => e.Participant)
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .Include(e => e.Result)
                .AsNoTracking()
                .ToListAsync();

            var dtos = enrolments.Select(e => new EnrolmentResponseDto
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

            return Ok(dtos);
        }

        /// <summary>
        /// Returns the logged-in participant's personal performance history across all past events.
        /// </summary>
        [HttpGet("me/results")]
        [Authorize(Roles = "Participant")]
        [SwaggerOperation(Summary = "Gets participant results history", Description = "Participant only. Returns all finish results recorded across all events.")]
        [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<ResultResponseDto>>> GetMyResults()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { error = "User is not authenticated." });
            }

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Participant)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Category)
                        .ThenInclude(c => c!.Event)
                .Where(r => r.Enrolment!.ParticipantId == userId.Value)
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
                CategoryName = r.Enrolment?.Category?.CategoryName,
                DistanceKm = r.Enrolment?.Category?.DistanceKm,
                EventName = r.Enrolment?.Category?.Event?.EventName,
                EventDate = r.Enrolment?.Category?.Event?.EventDate,
                Location = r.Enrolment?.Category?.Event?.Location
            });

            return Ok(dtos);
        }
    }

        // --- Requirement 4 & 5: Azure Blob Storage Profile Picture Upload ---
        /// <summary>
        /// Uploads a participant profile picture to Azure Blob Storage and attaches it to their user profile.
        /// </summary>
        [HttpPost("{id:int}/profile-picture")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Uploads a profile picture to Azure Blob Storage", Description = "Uploads image to Azure Blob container 'profile-pictures'.")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> UploadProfilePicture(
            int id, 
            IFormFile file,
            [FromServices] IAzureBlobService blobService)
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue && userId.Value != id)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "You can only update your own profile picture." });
            }

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound(new { error = $"User with ID {id} not found." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file was provided." });
            }

            using var stream = file.OpenReadStream();
            var blobUrl = await blobService.UploadFileAsync(stream, file.FileName, file.ContentType, "profile-pictures");

            user.ProfilePictureUrl = blobUrl;
            await _context.SaveChangesAsync();

            return Ok(new UserDto
            {
                UserId = user.UserId,
                RoleId = user.RoleId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Role = user.Role?.RoleName ?? "Participant",
                CreatedAt = user.CreatedAt
            });
        }
        // profile_upload_marker

}
