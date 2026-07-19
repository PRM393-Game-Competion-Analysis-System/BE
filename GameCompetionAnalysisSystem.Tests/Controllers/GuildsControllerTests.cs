using System.Collections.Generic;
using BIL.Service;
using DAL.DTO;
using DAL.Entities;
using GameCompetionAnalysisSystem.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Controllers
{
    public class GuildsControllerTests
    {
        private readonly Mock<IGuildService> _mockGuildService;
        private readonly GuildsController _controller;

        public GuildsControllerTests()
        {
            _mockGuildService = new Mock<IGuildService>();
            _controller = new GuildsController(_mockGuildService.Object);
        }

        [Fact]
        public void GetList_ReturnsOkResult_WithGuilds()
        {
            // Arrange
            var parameters = new QueryParameters();
            var expectedResult = new PagedResult<GuildDto>
            {
                Items = new List<GuildDto> { new GuildDto { GuildId = 1, GuildName = "TestGuild" } },
                TotalCount = 1
            };

            _mockGuildService.Setup(s => s.GetAll(parameters)).Returns(expectedResult);

            // Act
            var result = _controller.GetList(parameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<PagedResult<GuildDto>>(okResult.Value);
            Assert.Equal(1, actualResult.TotalCount);
        }

        [Fact]
        public void Search_ReturnsOkResult_WithMatchingGuilds()
        {
            // Arrange
            var searchName = "Test";
            var expectedResult = new List<GuildDto> { new GuildDto { GuildId = 1, GuildName = "TestGuild" } };

            _mockGuildService.Setup(s => s.SearchByName(searchName)).Returns(expectedResult);

            // Act
            var result = _controller.Search(searchName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<GuildDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsOkResult_WithGuild()
        {
            // Arrange
            int id = 1;
            var expectedGuild = new GuildDto { GuildId = id, GuildName = "TestGuild" };

            _mockGuildService.Setup(s => s.GetById(id)).Returns(expectedGuild);

            // Act
            var result = _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualGuild = Assert.IsType<GuildDto>(okResult.Value);
            Assert.Equal(id, actualGuild.GuildId);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            int id = 999;
            _mockGuildService.Setup(s => s.GetById(id)).Returns((GuildDto)null!);

            // Act
            var result = _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void GetByServer_ReturnsOkResult_WithGuilds()
        {
            // Arrange
            int serverId = 1;
            var expectedResult = new List<GuildDto> { new GuildDto { GuildId = 1, GuildName = "TestGuild" } };

            _mockGuildService.Setup(s => s.GetByServer(serverId)).Returns(expectedResult);

            // Act
            var result = _controller.GetByServer(serverId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<GuildDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void Create_ValidGuild_ReturnsOkResult_WithCreatedGuildDto()
        {
            // Arrange
            var guildToCreate = new Guild { Guildid = 1, Guildname = "NewGuild" };

            // Act
            var result = _controller.Create(guildToCreate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdGuildDto = Assert.IsType<GuildDto>(okResult.Value);
            Assert.Equal(guildToCreate.Guildid, createdGuildDto.GuildId);
            Assert.Equal(guildToCreate.Guildname, createdGuildDto.GuildName);
            
            _mockGuildService.Verify(s => s.Add(guildToCreate), Times.Once);
        }

        [Fact]
        public void Update_ValidGuild_ReturnsOkResult_WithUpdatedGuildDto()
        {
            // Arrange
            int id = 1;
            var guildToUpdate = new Guild { Guildname = "UpdatedGuild" };

            // Act
            var result = _controller.Update(id, guildToUpdate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedGuildDto = Assert.IsType<GuildDto>(okResult.Value);
            
            Assert.Equal(id, guildToUpdate.Guildid);
            Assert.Equal(id, updatedGuildDto.GuildId);
            Assert.Equal(guildToUpdate.Guildname, updatedGuildDto.GuildName);

            _mockGuildService.Verify(s => s.Update(guildToUpdate), Times.Once);
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
            _mockGuildService.Verify(s => s.Delete(id), Times.Once);
        }
    }
}
