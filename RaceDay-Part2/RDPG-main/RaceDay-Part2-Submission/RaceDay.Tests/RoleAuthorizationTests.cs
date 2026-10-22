using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using Xunit;

namespace RaceDay.Tests
{
    public class RoleAuthorizationTests
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
        public async Task CaptureResult_AsNonOrganiser_Returns403Forbidden()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(CaptureResult_AsNonOrganiser_Returns403Forbidden));
            var controller = new EnrolmentsController(context)
            {
                // User 3 is Lindiwe (Participant)
                ControllerContext = CreateContextWithUser(3, "Participant")
            };

            var dto = new CaptureResultDto
            {
                FinishTime = "00:59:12",
                OverallPosition = 5,
                Status = "Finished"
            };

            // Act: Trying to capture result on Enrolment 1 (owned by event 1, organiser 1)
            var result = await controller.CaptureResult(1, dto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
        }

        [Fact]
        public async Task CancelEnrolment_AsDifferentParticipant_Returns403Forbidden()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(CancelEnrolment_AsDifferentParticipant_Returns403Forbidden));
            var controller = new EnrolmentsController(context)
            {
                // User 4 is Pieter, but Enrolment 1 belongs to User 3 (Lindiwe)
                ControllerContext = CreateContextWithUser(4, "Participant")
            };

            // Act: Pieter trying to cancel Lindiwe's enrolment
            var result = await controller.CancelEnrolment(1);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
        }

        [Fact]
        public async Task AddCategory_AsNonOwnerOrganiser_Returns403Forbidden()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(AddCategory_AsNonOwnerOrganiser_Returns403Forbidden));
            var controller = new EventsController(context)
            {
                // User 2 is Johan, Event 1 belongs to Thandiwe (User 1)
                ControllerContext = CreateContextWithUser(2, "Organiser")
            };

            var dto = new CreateCategoryDto
            {
                CategoryName = "Unauthorized 5km",
                DistanceKm = 5.0m,
                EntryFee = 100
            };

            // Act
            var result = await controller.AddCategory(1, dto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
        }
    }
}
