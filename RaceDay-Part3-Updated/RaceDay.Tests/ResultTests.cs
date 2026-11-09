using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class ResultTests
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
        public async Task CaptureResult_AsOrganiserOwner_Returns201Created()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(CaptureResult_AsOrganiserOwner_Returns201Created));

            // Create a fresh enrolment for Event 1 (owned by Organiser 1 - Thandiwe) without a result
            var enrolment = new Enrolment
            {
                EnrolmentId = 99,
                ParticipantId = 4,
                CategoryId = 1,
                RaceNumber = "A999",
                PaymentStatus = "Paid"
            };
            context.Enrolments.Add(enrolment);
            context.SaveChanges();

            var controller = new EnrolmentsController(context)
            {
                ControllerContext = CreateContextWithUser(1, "Organiser")
            };

            var dto = new CaptureResultDto
            {
                FinishTime = "00:46:15",
                OverallPosition = 8,
                CategoryPosition = 3,
                Status = "Finished"
            };

            // Act
            var result = await controller.CaptureResult(99, dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var resDto = Assert.IsType<ResultResponseDto>(createdResult.Value);

            Assert.Equal("00:46:15", resDto.FinishTime);
            Assert.Equal(8, resDto.OverallPosition);
            Assert.Equal(3, resDto.CategoryPosition);
            Assert.Equal("Finished", resDto.Status);

            // Verify in DB
            var dbResult = context.Results.FirstOrDefault(r => r.EnrolmentId == 99);
            Assert.NotNull(dbResult);
            Assert.Equal(new TimeSpan(0, 46, 15), dbResult.FinishTime);
        }

        [Fact]
        public async Task CaptureResult_DuplicateForSameEnrolment_Returns409Conflict()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(CaptureResult_DuplicateForSameEnrolment_Returns409Conflict));
            var controller = new EnrolmentsController(context)
            {
                // Enrolment 1 ALREADY has Result 1 in the seed data
                ControllerContext = CreateContextWithUser(1, "Organiser")
            };

            var dto = new CaptureResultDto
            {
                FinishTime = "00:55:00",
                OverallPosition = 40,
                Status = "Finished"
            };

            // Act
            var result = await controller.CaptureResult(1, dto);

            // Assert
            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateResult_AsOrganiserOwner_Returns200Ok()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(UpdateResult_AsOrganiserOwner_Returns200Ok));
            var controller = new ResultsController(context)
            {
                // Result 1 belongs to Enrolment 1 -> Event 1 -> Organiser 1 (Thandiwe)
                ControllerContext = CreateContextWithUser(1, "Organiser")
            };

            var dto = new UpdateResultDto
            {
                FinishTime = "00:51:30",
                OverallPosition = 32
            };

            // Act
            var result = await controller.UpdateResult(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resDto = Assert.IsType<ResultResponseDto>(okResult.Value);

            Assert.Equal("00:51:30", resDto.FinishTime);
            Assert.Equal(32, resDto.OverallPosition);
        }
    }
}
