using BIL.Service;
using DAL.Entities;
using Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System.Linq;

namespace GameCompetionAnalysisSystem.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly PRM393GameAiContext _context;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            
            _context = new PRM393GameAiContext(options);
            _controller = new AuthController(_mockAuthService.Object, _context);
        }

        [Fact]
        public void Login_ValidCredentials_ReturnsOkResult_WithTokenAndUser()
        {
            // Arrange
            var loginRequest = new LoginRequest { Email = "test@example.com", Password = "password123" };
            var user = new User { Userid = 1, Username = "TestUser", Email = "test@example.com", Role = "user" };
            var expectedToken = "fake-jwt-token";

            _mockAuthService.Setup(s => s.Authenticate(loginRequest.Email, loginRequest.Password)).Returns(user);
            _mockAuthService.Setup(s => s.GenerateJwtToken(user)).Returns(expectedToken);

            // Act
            var result = _controller.Login(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // We can check if token is present via dynamic or reflection, but OkObjectResult means success.
            var valueType = okResult.Value.GetType();
            var tokenProp = valueType.GetProperty("Token");
            Assert.NotNull(tokenProp);
            Assert.Equal(expectedToken, tokenProp.GetValue(okResult.Value, null));
        }

        [Fact]
        public void Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new LoginRequest { Email = "test@example.com", Password = "wrongpassword" };

            _mockAuthService.Setup(s => s.Authenticate(loginRequest.Email, loginRequest.Password)).Returns((User)null!);

            // Act
            var result = _controller.Login(loginRequest);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorizedResult.Value);
        }

        [Fact]
        public void Register_NewUser_ReturnsOkResult_SavesUserToDatabase()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Username = "NewUser",
                Email = "newuser@example.com",
                Password = "password123"
            };

            // Act
            var result = _controller.Register(registerRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var userInDb = _context.Users.FirstOrDefault(u => u.Email == "newuser@example.com");
            Assert.NotNull(userInDb);
            Assert.Equal("NewUser", userInDb.Username);
            Assert.Equal("password123", userInDb.Passwordhash);
            Assert.Equal("user", userInDb.Role);
        }

        [Fact]
        public void Register_ExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var existingUser = new User { Username = "ExistingUser", Email = "existing@example.com" };
            _context.Users.Add(existingUser);
            _context.SaveChanges();

            var registerRequest = new RegisterRequest
            {
                Username = "NewUser",
                Email = "existing@example.com",
                Password = "password123"
            };

            // Act
            var result = _controller.Register(registerRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public void Register_ExistingUsername_ReturnsBadRequest()
        {
            // Arrange
            var existingUser = new User { Username = "ExistingUser", Email = "existing@example.com" };
            _context.Users.Add(existingUser);
            _context.SaveChanges();

            var registerRequest = new RegisterRequest
            {
                Username = "ExistingUser",
                Email = "new@example.com",
                Password = "password123"
            };

            // Act
            var result = _controller.Register(registerRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }
    }
}
