using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class EnrolmentTests
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
        public async Task Enrol_ValidParticipant_GeneratesRaceNumberAndReturns201()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(Enrol_ValidParticipant_GeneratesRaceNumberAndReturns201));
            var controller = new CategoriesController(context)
            {
                // User 4 is Pieter Botha (Participant), enrolling in Category 1 (10km Fun Run)
                ControllerContext = CreateContextWithUser(4, "Participant")
            };

            var dto = new EnrolDto { PaymentStatus = "Paid" };

            // Act: Category 1 (10km Fun Run)
            var result = await controller.Enrol(1, dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var enrolment = Assert.IsType<EnrolmentResponseDto>(createdResult.Value);

            Assert.Equal(4, enrolment.ParticipantId);
            Assert.Equal(1, enrolment.CategoryId);
            Assert.NotNull(enrolment.RaceNumber);
            Assert.StartsWith("C", enrolment.RaceNumber); // Distance 10km gets 'C' prefix
            Assert.Equal("Paid", enrolment.PaymentStatus);

            // Verify persisted in DB
            var persisted = context.Enrolments.FirstOrDefault(e => e.ParticipantId == 4 && e.CategoryId == 1);
            Assert.NotNull(persisted);
        }

        [Fact]
        public async Task Enrol_DuplicateCategory_Returns409Conflict()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(Enrol_DuplicateCategory_Returns409Conflict));
            var controller = new CategoriesController(context)
            {
                // User 3 (Lindiwe) is ALREADY enrolled in Category 1 in the seed data
                ControllerContext = CreateContextWithUser(3, "Participant")
            };

            var dto = new EnrolDto { PaymentStatus = "Paid" };

            // Act
            var result = await controller.Enrol(1, dto);

            // Assert
            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Enrol_CategoryFull_Returns400BadRequest()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(Enrol_CategoryFull_Returns400BadRequest));

            // Set capacity of Category 5 to 1 (which Pieter Botha is already enrolled in)
            var cat5 = context.Categories.Find(5);
            cat5!.MaxParticipants = 1;
            context.SaveChanges();

            var controller = new CategoriesController(context)
            {
                // User 3 (Lindiwe) tries to enrol in full category 5
                ControllerContext = CreateContextWithUser(3, "Participant")
            };

            // Act
            var result = await controller.Enrol(5, new EnrolDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task WithdrawEnrolment_AsOwner_Returns204NoContent()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(WithdrawEnrolment_AsOwner_Returns204NoContent));
            var controller = new EnrolmentsController(context)
            {
                // Enrolment 1 belongs to User 3 (Lindiwe)
                ControllerContext = CreateContextWithUser(3, "Participant")
            };

            // Act
            var result = await controller.CancelEnrolment(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(context.Enrolments.Find(1));
        }
    }
}
