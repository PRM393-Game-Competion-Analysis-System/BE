using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using BIL.Service;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly PRM393GameAiContext _context;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();

            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PRM393GameAiContext(options);
            _authService = new AuthService(_context, _mockConfiguration.Object);
        }

        [Fact]
        public void Authenticate_ValidCredentials_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                Passwordhash = "password123",
                Role = "user"
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = _authService.Authenticate("test@example.com", "password123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public void Authenticate_InvalidCredentials_ReturnsNull()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                Passwordhash = "password123",
                Role = "user"
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = _authService.Authenticate("test@example.com", "wrongpassword");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Authenticate_NonExistentEmail_ReturnsNull()
        {
            // Act
            var result = _authService.Authenticate("notfound@example.com", "password123");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GenerateJwtToken_ValidUser_ReturnsTokenString()
        {
            // Arrange
            var user = new User
            {
                Userid = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = "user"
            };

            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?> {
                {"Jwt:Key", "SuperSecretKeyThatIsAtLeast32BytesLong123!"},
                {"Jwt:DurationInMinutes", "60"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"}
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var authService = new AuthService(_context, configuration);

            // Act
            var token = authService.GenerateJwtToken(user);

            // Assert
            Assert.False(string.IsNullOrEmpty(token));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            Assert.Equal("TestIssuer", jwtToken.Issuer);
            Assert.Equal("TestAudience", jwtToken.Audiences.First());
            Assert.Equal("1", jwtToken.Claims.First(c => c.Type == "UserId").Value);
        }

        [Fact]
        public void GenerateJwtToken_MissingJwtKey_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = new User
            {
                Userid = 1,
                Username = "testuser"
            };

            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?> {
                {"Jwt:DurationInMinutes", "60"}
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var authService = new AuthService(_context, configuration);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => authService.GenerateJwtToken(user));
            Assert.Equal("JWT Key is not configured", exception.Message);
        }
    }
}
