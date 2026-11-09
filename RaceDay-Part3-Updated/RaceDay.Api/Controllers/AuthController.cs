using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokenService;

        public AuthController(RaceDayDbContext context, IPasswordHasher hasher, ITokenService tokenService)
        {
            _context = context;
            _hasher = hasher;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Registers a new user with a chosen role (Organiser or Participant).
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Registers a new user", Description = "Creates a new user profile with hashed password and role assignment.")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await _context.Roles.FindAsync(dto.RoleId);
            if (role == null)
            {
                return BadRequest(new { error = $"Unknown roleId {dto.RoleId}. Must be 1 (Organiser) or 2 (Participant)." });
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (existingUser != null)
            {
                return Conflict(new { error = "Email is already registered." });
            }

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PasswordHash = _hasher.HashPassword(dto.Password),
                RoleId = dto.RoleId,
                Phone = dto.Phone?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Set session (Requirement 2)
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", role.RoleName);

            var userDto = new UserDto
            {
                UserId = user.UserId,
                RoleId = user.RoleId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = role.RoleName,
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(UsersController.GetMe), "Users", null, userDto);
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token for subsequent requests.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Logs in a user", Description = "Validates credentials, initializes session state, and returns a signed JWT token.")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null || !_hasher.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { error = "Invalid email or password." });
            }

            var roleName = user.Role?.RoleName ?? (user.RoleId == 1 ? "Organiser" : "Participant");

            // Session state management
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", roleName);

            var token = _tokenService.GenerateToken(user, roleName);

            var userDto = new UserDto
            {
                UserId = user.UserId,
                RoleId = user.RoleId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = roleName,
                CreatedAt = user.CreatedAt
            };

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = userDto
            });
        }
    }
}
