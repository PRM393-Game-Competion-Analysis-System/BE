using System.Collections.Generic;
using System.Linq;
using BIL.Service;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockRepo;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _mockRepo = new Mock<IUserRepository>();
            _service = new UserService(_mockRepo.Object);
        }

        [Fact]
        public void GetAll_ReturnsPagedResultOfUserDto()
        {
            // Arrange
            var parameters = new QueryParameters { PageNumber = 1, PageSize = 10 };
            var users = new List<User>
            {
                new User { Userid = 1, Username = "User1", Email = "user1@example.com", Role = "user" },
                new User { Userid = 2, Username = "User2", Email = "user2@example.com", Role = "admin" }
            };
            int expectedTotalCount = 2;

            _mockRepo.Setup(r => r.GetAll(parameters, out expectedTotalCount)).Returns(users);

            // Act
            var result = _service.GetAll(parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("User1", result.Items.First().Username);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsUserDto()
        {
            // Arrange
            int id = 1;
            var user = new User { Userid = id, Username = "User1", Email = "user1@example.com" };
            _mockRepo.Setup(r => r.GetById(id)).Returns(user);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.UserId);
            Assert.Equal("User1", result.Username);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNull()
        {
            // Arrange
            int id = 99;
            _mockRepo.Setup(r => r.GetById(id)).Returns((User)null!);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Update_CallsRepositoryUpdate()
        {
            // Arrange
            var user = new User { Userid = 1, Username = "UpdatedUser" };

            // Act
            _service.Update(user);

            // Assert
            _mockRepo.Verify(r => r.Update(user), Times.Once);
        }

        [Fact]
        public void UpdateProfile_ExistingUser_UpdatesFieldsAndCallsRepoUpdate()
        {
            // Arrange
            int userId = 1;
            var user = new User { Userid = userId, Username = "OldName", Email = "old@example.com" };
            var dto = new UpdateProfileDto { Username = "NewName", Email = "new@example.com" };

            _mockRepo.Setup(r => r.GetById(userId)).Returns(user);

            // Act
            _service.UpdateProfile(userId, dto);

            // Assert
            Assert.Equal("NewName", user.Username);
            Assert.Equal("new@example.com", user.Email);
            _mockRepo.Verify(r => r.Update(user), Times.Once);
        }

        [Fact]
        public void UpdateProfile_NonExistingUser_DoesNothing()
        {
            // Arrange
            int userId = 99;
            var dto = new UpdateProfileDto { Username = "NewName" };

            _mockRepo.Setup(r => r.GetById(userId)).Returns((User)null!);

            // Act
            _service.UpdateProfile(userId, dto);

            // Assert
            _mockRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public void Delete_CallsRepositoryDelete()
        {
            // Arrange
            int id = 1;

            // Act
            _service.Delete(id);

            // Assert
            _mockRepo.Verify(r => r.Delete(id), Times.Once);
        }

        [Fact]
        public void Create_ValidDto_ReturnsCreatedUserDtoAndCallsAdd()
        {
            // Arrange
            var dto = new CreateUserDto { Username = "NewUser", Email = "new@example.com", Password = "pwd" };

            // Act
            var result = _service.Create(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NewUser", result.Username);
            Assert.Equal("user", result.Role); // Default role
            _mockRepo.Verify(r => r.Add(It.Is<User>(u => u.Username == "NewUser")), Times.Once);
        }
    }
}
