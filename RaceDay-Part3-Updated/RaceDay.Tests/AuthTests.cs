using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;
using Xunit;

namespace RaceDay.Tests
{
    public class AuthTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly PasswordHasher _passwordHasher;

        public AuthTests()
        {
            _tokenServiceMock = new Mock<ITokenService>();
            _tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<RaceDay.Api.Models.User>(), It.IsAny<string>()))
                .Returns("mocked_jwt_token_for_test");
            _passwordHasher = new PasswordHasher();
        }

        [Fact]
        public async Task Register_ValidParticipant_Returns201CreatedAndHashedPassword()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(Register_ValidParticipant_Returns201CreatedAndHashedPassword));
            var controller = new AuthController(context, _passwordHasher, _tokenServiceMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var dto = new RegisterDto
            {
                FullName = "Sipho Khumalo",
                Email = "sipho.khumalo@gmail.com",
                Password = "SecurePassword123!",
                RoleId = 2, // Participant
                Phone = "0823334444"
            };

            // Act
            var actionResult = await controller.Register(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var userDto = Assert.IsType<UserDto>(createdResult.Value);

            Assert.Equal("Sipho Khumalo", userDto.FullName);
            Assert.Equal("sipho.khumalo@gmail.com", userDto.Email);
            Assert.Equal("Participant", userDto.Role);

            // Verify password was securely hashed (not plain text)
            var savedUser = context.Users.First(u => u.Email == dto.Email);
            Assert.NotEqual("SecurePassword123!", savedUser.PasswordHash);
            Assert.True(_passwordHasher.VerifyPassword("SecurePassword123!", savedUser.PasswordHash));
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns409Conflict()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(Register_DuplicateEmail_Returns409Conflict));
            var controller = new AuthController(context, _passwordHasher, _tokenServiceMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var dto = new RegisterDto
            {
                FullName = "Duplicate Runner",
                Email = "thandiwe@raceday.co.za", // already seeded in DB
                Password = "Password123",
                RoleId = 2
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Register_UnknownRoleId_Returns400BadRequest()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(Register_UnknownRoleId_Returns400BadRequest));
            var controller = new AuthController(context, _passwordHasher, _tokenServiceMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var dto = new RegisterDto
            {
                FullName = "Invalid Role User",
                Email = "invalid.role@example.com",
                Password = "Password123",
                RoleId = 99 // Non-existent role
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_ValidCredentials_Returns200AndToken()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(Login_ValidCredentials_Returns200AndToken));
            var controller = new AuthController(context, _passwordHasher, _tokenServiceMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Thandiwe is seeded with role 1
            var dto = new LoginDto
            {
                Email = "thandiwe@raceday.co.za",
                Password = "password123" // Matches seeded BCrypt hash in RaceDayDbContext
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var authResponse = Assert.IsType<AuthResponseDto>(okResult.Value);

            Assert.Equal("mocked_jwt_token_for_test", authResponse.Token);
            Assert.Equal("Thandiwe Mokoena", authResponse.User.FullName);
            Assert.Equal("Organiser", authResponse.User.Role);
        }

        [Fact]
        public async Task Login_NonExistentEmail_Returns401Unauthorized()
        {
            // Arrange
            using var context = TestDbHelper.GetInMemoryDbContext(nameof(Login_NonExistentEmail_Returns401Unauthorized));
            var controller = new AuthController(context, _passwordHasher, _tokenServiceMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var dto = new LoginDto
            {
                Email = "unknown.ghost@raceday.co.za",
                Password = "Password123"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }
    }
}
