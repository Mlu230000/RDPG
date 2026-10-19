using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class EventManagementTests
    {
        private ControllerContext CreateContextWithUser(int userId, string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuth"));

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_Returns201Created()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(CreateEvent_AsOrganiser_Returns201Created));
            var controller = new EventsController(context)
            {
                ControllerContext = CreateContextWithUser(1, "Organiser")
            };

            var dto = new CreateEventDto
            {
                EventName = "Drakensberg Escarpment Trail",
                Description = "Mountain trail running challenge",
                EventDate = new DateTime(2027, 4, 15),
                StartTime = "06:30",
                Location = "Royal Natal National Park",
                Province = "KwaZulu-Natal",
                EventTypeId = 1 // Running
            };

            // Act
            var result = await controller.CreateEvent(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var ev = Assert.IsType<Event>(createdResult.Value);

            Assert.Equal("Drakensberg Escarpment Trail", ev.EventName);
            Assert.Equal(1, ev.OrganiserId);
            Assert.Equal("Upcoming", ev.Status);
        }

        [Fact]
        public async Task UpdateEvent_AsOwnerOrganiser_Returns200Ok()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(UpdateEvent_AsOwnerOrganiser_Returns200Ok));
            var controller = new EventsController(context)
            {
                ControllerContext = CreateContextWithUser(1, "Organiser") // Event 1 owner is Thandiwe (user 1)
            };

            var dto = new UpdateEventDto
            {
                EventName = "Soweto Marathon - 30th Anniversary Edition",
                Status = "Open"
            };

            // Act
            var result = await controller.UpdateEvent(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var ev = Assert.IsType<Event>(okResult.Value);

            Assert.Equal("Soweto Marathon - 30th Anniversary Edition", ev.EventName);
            Assert.Equal("Open", ev.Status);
        }

        [Fact]
        public async Task UpdateEvent_AsNonOwner_Returns403Forbidden()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(UpdateEvent_AsNonOwner_Returns403Forbidden));
            var controller = new EventsController(context)
            {
                ControllerContext = CreateContextWithUser(2, "Organiser") // User 2 is Johan, but Event 1 is owned by Thandiwe (user 1)
            };

            var dto = new UpdateEventDto
            {
                EventName = "Unauthorized Rename"
            };

            // Act
            var result = await controller.UpdateEvent(1, dto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_WithPaidEnrolments_Returns409Conflict()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(DeleteEvent_WithPaidEnrolments_Returns409Conflict));
            var controller = new EventsController(context)
            {
                ControllerContext = CreateContextWithUser(1, "Organiser") // Event 1 has paid enrolments in seed data
            };

            // Act
            var result = await controller.DeleteEvent(1);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task DeleteEvent_WithoutPaidEnrolments_Returns204NoContent()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(DeleteEvent_WithoutPaidEnrolments_Returns204NoContent));

            // Create a fresh event with no enrolments
            var newEvent = new Event
            {
                EventId = 99,
                OrganiserId = 1,
                EventTypeId = 1,
                EventName = "Empty Event",
                EventDate = DateTime.UtcNow.AddMonths(2),
                Location = "Pretoria",
                Status = "Upcoming"
            };
            context.Events.Add(newEvent);
            await context.SaveChangesAsync();

            var controller = new EventsController(context)
            {
                ControllerContext = CreateContextWithUser(1, "Organiser")
            };

            // Act
            var result = await controller.DeleteEvent(99);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Events.FindAsync(99));
        }
    }
}
