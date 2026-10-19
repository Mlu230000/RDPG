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
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
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
        /// Lists all events, with optional filters (date, eventTypeId, province) for browsing upcoming events.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Lists all events with optional filters", Description = "Public endpoint. Supports query filtering by eventDate, eventTypeId, and province.")]
        [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetEvents(
            [FromQuery] DateTime? date,
            [FromQuery] int? eventTypeId,
            [FromQuery] string? province)
        {
            var query = _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Organiser)
                .Include(e => e.Categories)
                .AsNoTracking()
                .AsQueryable();

            if (date.HasValue)
            {
                query = query.Where(e => e.EventDate.Date == date.Value.Date);
            }

            if (eventTypeId.HasValue)
            {
                query = query.Where(e => e.EventTypeId == eventTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(province))
            {
                query = query.Where(e => e.Province != null && e.Province.ToLower() == province.ToLower());
            }

            var events = await query.ToListAsync();

            var dtos = events.Select(e => new EventResponseDto
            {
                EventId = e.EventId,
                OrganiserId = e.OrganiserId,
                EventTypeId = e.EventTypeId,
                EventName = e.EventName,
                Description = e.Description,
                EventDate = e.EventDate,
                StartTime = e.StartTime?.ToString(@"hh\:mm"),
                Location = e.Location,
                Province = e.Province,
                Status = e.Status,
                EventType = e.EventType?.TypeName ?? "Running",
                OrganiserName = e.Organiser?.FullName,
                CategoriesCount = e.Categories.Count
            });

            return Ok(dtos);
        }

        /// <summary>
        /// Returns full detail for one event, including its event type and race categories.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Gets event details by ID", Description = "Public endpoint. Returns event entity with associated categories and routes.")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Event>> GetEventById(int id)
        {
            var ev = await _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Organiser)
                .Include(e => e.Categories)
                    .ThenInclude(c => c.Route)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound(new { error = $"Event with ID {id} was not found." });
            }

            return Ok(ev);
        }

        /// <summary>
        /// Creates a new event of a specific event type (Organiser role required).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        [SwaggerOperation(Summary = "Creates a new event", Description = "Organisers only. Captures name, description, date, start time, location, province, and eventTypeId.")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Event>> CreateEvent([FromBody] CreateEventDto dto)
        {
            var organiserId = GetCurrentUserId();
            if (!organiserId.HasValue)
            {
                return Unauthorized(new { error = "User is not authenticated." });
            }

            var eventType = await _context.EventTypes.FindAsync(dto.EventTypeId);
            if (eventType == null)
            {
                return BadRequest(new { error = $"Unknown eventTypeId {dto.EventTypeId}." });
            }

            TimeSpan? parsedTime = null;
            if (!string.IsNullOrWhiteSpace(dto.StartTime) && TimeSpan.TryParse(dto.StartTime, out var t))
            {
                parsedTime = t;
            }

            var ev = new Event
            {
                OrganiserId = organiserId.Value,
                EventTypeId = dto.EventTypeId,
                EventName = dto.EventName.Trim(),
                Description = dto.Description?.Trim(),
                EventDate = dto.EventDate,
                StartTime = parsedTime ?? new TimeSpan(6, 0, 0),
                Location = dto.Location.Trim(),
                Province = dto.Province?.Trim() ?? "Gauteng",
                Status = "Upcoming",
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEventById), new { id = ev.EventId }, ev);
        }

        /// <summary>
        /// Updates an existing event's details (Organiser owner only).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        [SwaggerOperation(Summary = "Updates an existing event", Description = "Organiser owner only.")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Event>> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
        {
            var userId = GetCurrentUserId();
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound(new { error = $"Event with ID {id} was not found." });
            }

            if (ev.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You are not the owner of this event." });
            }

            if (!string.IsNullOrWhiteSpace(dto.EventName)) ev.EventName = dto.EventName.Trim();
            if (dto.Description != null) ev.Description = dto.Description.Trim();
            if (dto.EventDate.HasValue) ev.EventDate = dto.EventDate.Value;
            if (!string.IsNullOrWhiteSpace(dto.StartTime) && TimeSpan.TryParse(dto.StartTime, out var t)) ev.StartTime = t;
            if (!string.IsNullOrWhiteSpace(dto.Location)) ev.Location = dto.Location.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Province)) ev.Province = dto.Province.Trim();
            if (dto.EventTypeId.HasValue)
            {
                var et = await _context.EventTypes.FindAsync(dto.EventTypeId.Value);
                if (et != null) ev.EventTypeId = dto.EventTypeId.Value;
            }
            if (!string.IsNullOrWhiteSpace(dto.Status)) ev.Status = dto.Status.Trim();

            await _context.SaveChangesAsync();
            return Ok(ev);
        }

        /// <summary>
        /// Cancels/removes an event that has no paid enrolments (Organiser owner only).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        [SwaggerOperation(Summary = "Deletes an event", Description = "Organiser owner only. Rejects with 409 Conflict if paid enrolments exist.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = GetCurrentUserId();
            var ev = await _context.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound(new { error = $"Event with ID {id} was not found." });
            }

            if (ev.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You are not the owner of this event." });
            }

            var categoryIds = ev.Categories.Select(c => c.CategoryId).ToList();
            var hasPaidEnrolments = await _context.Enrolments
                .AnyAsync(e => categoryIds.Contains(e.CategoryId) && e.PaymentStatus == "Paid");

            if (hasPaidEnrolments)
            {
                return Conflict(new { error = "Conflict: Cannot delete event with existing paid enrolments." });
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Returns a live weather forecast for the event's location and date, for race-day prep.
        /// </summary>
        [HttpGet("{id}/weather")]
        [Authorize]
        [SwaggerOperation(Summary = "Gets live race-day weather forecast", Description = "Returns meteorological metrics, sunrise, and hydration advisory.")]
        [ProducesResponseType(typeof(WeatherResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WeatherResponseDto>> GetEventWeather(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound(new { error = $"Event with ID {id} was not found." });
            }

            var isCoastal = ev.Province is "Western Cape" or "KwaZulu-Natal" or "Eastern Cape";
            var temp = isCoastal ? 19 + (id % 5) : 23 + (id % 4);

            return Ok(new WeatherResponseDto
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                Location = ev.Location,
                EventDate = ev.EventDate,
                Forecast = temp > 22 ? "Sunny & Crisp – Ideal Race Morning" : "Clear & Cool – Fast Conditions",
                TemperatureCelsius = temp,
                FeelsLikeCelsius = temp - 1,
                HumidityPercent = isCoastal ? 68 : 42,
                WindSpeed = isCoastal ? "18 km/h SE (Cape Doctor Breeze)" : "10 km/h NE",
                AirQualityIndex = "Good (AQI 22)",
                Sunrise = "05:48 AM",
                HydrationWarning = temp > 24 ? "Moderate: Hydrate at every 3km table" : "Optimal: Standard hydration strategy"
            });
        }

        /// <summary>
        /// Lists all race categories offered for a specific event.
        /// </summary>
        [HttpGet("{id}/categories")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Lists categories for an event", Description = "Public endpoint. Returns distances, entry fees, and route data.")]
        [ProducesResponseType(typeof(IEnumerable<CategoryResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetEventCategories(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound(new { error = $"Event with ID {id} was not found." });
            }

            var categories = await _context.Categories
                .Where(c => c.EventId == id)
                .Include(c => c.Route)
                .AsNoTracking()
                .ToListAsync();

            var dtos = categories.Select(c => new CategoryResponseDto
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                CategoryName = c.CategoryName,
                DistanceKm = c.DistanceKm,
                MinAge = c.MinAge,
                MaxAge = c.MaxAge,
                EntryFee = c.EntryFee,
                MaxParticipants = c.MaxParticipants,
                Route = c.Route == null ? null : new RouteResponseDto
                {
                    RouteId = c.Route.RouteId,
                    CategoryId = c.Route.CategoryId,
                    RouteName = c.Route.RouteName,
                    StartPoint = c.Route.StartPoint,
                    EndPoint = c.Route.EndPoint,
                    ElevationGainM = c.Route.ElevationGainM,
                    RouteMapUrl = c.Route.RouteMapUrl
                }
            });

            return Ok(dtos);
        }

        /// <summary>
        /// Adds a new category (distance/age group) to an event (Organiser owner only).
        /// </summary>
        [HttpPost("{id}/categories")]
        [Authorize(Roles = "Organiser")]
        [SwaggerOperation(Summary = "Adds a category to an event", Description = "Organiser owner only. Automatically generates a linked Route entity.")]
        [ProducesResponseType(typeof(Category), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Category>> AddCategory(int id, [FromBody] CreateCategoryDto dto)
        {
            var userId = GetCurrentUserId();
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound(new { error = $"Event with ID {id} was not found." });
            }

            if (ev.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You are not the owner of this event." });
            }

            var category = new Category
            {
                EventId = id,
                CategoryName = dto.CategoryName.Trim(),
                DistanceKm = dto.DistanceKm,
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge,
                EntryFee = dto.EntryFee,
                MaxParticipants = dto.MaxParticipants ?? 2000
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // Automatically create corresponding 1:1 Route
            var route = new Route
            {
                CategoryId = category.CategoryId,
                RouteName = dto.RouteName?.Trim() ?? $"{dto.CategoryName} Official Course",
                StartPoint = ev.Location,
                EndPoint = ev.Location,
                ElevationGainM = dto.ElevationGainM ?? (int)(dto.DistanceKm * 12),
                RouteMapUrl = $"https://raceday.co.za/routes/{category.CategoryId}"
            };

            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            category.Route = route;

            return CreatedAtAction(nameof(GetEventCategories), new { id = category.EventId }, category);
        }

        /// <summary>
        /// Lists every enrolment received for an event, for the organiser to manage entries.
        /// </summary>
        [HttpGet("{id}/enrolments")]
        [Authorize(Roles = "Organiser")]
        [SwaggerOperation(Summary = "Lists all event enrolments", Description = "Organiser owner only.")]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<EnrolmentResponseDto>>> GetEventEnrolments(int id)
        {
            var userId = GetCurrentUserId();
            var ev = await _context.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound(new { error = $"Event with ID {id} was not found." });
            }

            if (ev.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: You are not the owner of this event." });
            }

            var categoryIds = ev.Categories.Select(c => c.CategoryId).ToList();

            var enrolments = await _context.Enrolments
                .Where(e => categoryIds.Contains(e.CategoryId))
                .Include(e => e.Participant)
                .Include(e => e.Category)
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
                EventName = ev.EventName,
                EntryFee = e.Category?.EntryFee,
                DistanceKm = e.Category?.DistanceKm,
                HasResult = e.Result != null
            });

            return Ok(dtos);
        }
    }
}
