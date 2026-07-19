using System.Collections.Generic;
using System.Security.Claims;
using BIL.Service;
using DAL.DTO;
using DAL.Entities;
using GameCompetionAnalysisSystem.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _controller = new UsersController(_mockUserService.Object);
        }

        private void SetUserContext(string userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("UserId", userId),
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        private void SetEmptyUserContext()
        {
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal() }
            };
        }

        [Fact]
        public void GetList_ReturnsOkResult_WithUsers()
        {
            // Arrange
            var parameters = new QueryParameters();
            var expectedResult = new PagedResult<UserDto>
            {
                Items = new List<UserDto> { new UserDto { UserId = 1, Username = "TestUser" } },
                TotalCount = 1
            };

            _mockUserService.Setup(s => s.GetAll(parameters)).Returns(expectedResult);

            // Act
            var result = _controller.GetList(parameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<PagedResult<UserDto>>(okResult.Value);
            Assert.Equal(1, actualResult.TotalCount);
        }

        [Fact]
        public void Create_ValidUser_ReturnsOkResult_WithCreatedUserDto()
        {
            // Arrange
            var userToCreate = new CreateUserDto { Username = "NewUser", Password = "pwd" };
            var expectedUserDto = new UserDto { UserId = 1, Username = "NewUser" };

            _mockUserService.Setup(s => s.Create(userToCreate)).Returns(expectedUserDto);

            // Act
            var result = _controller.Create(userToCreate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdUserDto = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(expectedUserDto.UserId, createdUserDto.UserId);
        }

        [Fact]
        public void GetProfile_WithValidUserId_ReturnsOkResult_WithProfile()
        {
            // Arrange
            SetUserContext("1");
            var expectedUserDto = new UserDto { UserId = 1, Username = "TestUser" };
            _mockUserService.Setup(s => s.GetById(1)).Returns(expectedUserDto);

            // Act
            var result = _controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualProfile = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(1, actualProfile.UserId);
        }

        [Fact]
        public void GetProfile_WithInvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            SetUserContext("invalid_id");

            // Act
            var result = _controller.GetProfile();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public void GetProfile_WithMissingUserId_ReturnsUnauthorized()
        {
            // Arrange
            SetEmptyUserContext();

            // Act
            var result = _controller.GetProfile();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public void GetProfile_WithNonExistingUser_ReturnsNotFound()
        {
            // Arrange
            SetUserContext("999");
            _mockUserService.Setup(s => s.GetById(999)).Returns((UserDto)null!);

            // Act
            var result = _controller.GetProfile();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void UpdateProfile_WithValidUserId_ReturnsOkResult()
        {
            // Arrange
            SetUserContext("1");
            var profileToUpdate = new UpdateProfileDto { Username = "Updated Name" };

            // Act
            var result = _controller.UpdateProfile(profileToUpdate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockUserService.Verify(s => s.UpdateProfile(1, profileToUpdate), Times.Once);
        }

        [Fact]
        public void UpdateProfile_WithInvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            SetUserContext("invalid_id");
            var profileToUpdate = new UpdateProfileDto { Username = "Updated Name" };

            // Act
            var result = _controller.UpdateProfile(profileToUpdate);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockUserService.Verify(s => s.UpdateProfile(It.IsAny<int>(), It.IsAny<UpdateProfileDto>()), Times.Never);
        }

        [Fact]
        public void Delete_ReturnsOkResult()
        {
            // Arrange
            int id = 1;

            // Act
            var result = _controller.Delete(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockUserService.Verify(s => s.Delete(id), Times.Once);
        }
    }
}
