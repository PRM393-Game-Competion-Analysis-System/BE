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
    public class PlayersControllerTests
    {
        private readonly Mock<IPlayerService> _mockPlayerService;
        private readonly PlayersController _controller;

        public PlayersControllerTests()
        {
            _mockPlayerService = new Mock<IPlayerService>();
            _controller = new PlayersController(_mockPlayerService.Object);
        }

        [Fact]
        public void GetList_ReturnsOkResult_WithPlayers()
        {
            // Arrange
            var parameters = new QueryParameters();
            var expectedResult = new PagedResult<PlayerDto>
            {
                Items = new List<PlayerDto> { new PlayerDto { PlayerId = 1, PlayerName = "TestPlayer" } },
                TotalCount = 1
            };

            _mockPlayerService.Setup(s => s.GetAll(parameters)).Returns(expectedResult);

            // Act
            var result = _controller.GetList(parameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<PagedResult<PlayerDto>>(okResult.Value);
            Assert.Equal(1, actualResult.TotalCount);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsOkResult_WithPlayer()
        {
            // Arrange
            int id = 1;
            var expectedPlayer = new PlayerDto { PlayerId = id, PlayerName = "TestPlayer" };

            _mockPlayerService.Setup(s => s.GetById(id)).Returns(expectedPlayer);

            // Act
            var result = _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualPlayer = Assert.IsType<PlayerDto>(okResult.Value);
            Assert.Equal(id, actualPlayer.PlayerId);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            int id = 999;
            _mockPlayerService.Setup(s => s.GetById(id)).Returns((PlayerDto)null!);

            // Act
            var result = _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Search_ReturnsOkResult_WithMatchingPlayers()
        {
            // Arrange
            var searchName = "Test";
            var expectedResult = new List<PlayerDto> { new PlayerDto { PlayerId = 1, PlayerName = "TestPlayer" } };

            _mockPlayerService.Setup(s => s.SearchByName(searchName)).Returns(expectedResult);

            // Act
            var result = _controller.Search(searchName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<PlayerDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void GetByGame_ReturnsOkResult_WithPlayers()
        {
            // Arrange
            int gameId = 1;
            var expectedResult = new List<PlayerDto> { new PlayerDto { PlayerId = 1, PlayerName = "TestPlayer" } };

            _mockPlayerService.Setup(s => s.GetByGame(gameId)).Returns(expectedResult);

            // Act
            var result = _controller.GetByGame(gameId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<PlayerDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void GetByServer_ReturnsOkResult_WithPlayers()
        {
            // Arrange
            int serverId = 1;
            var expectedResult = new List<PlayerDto> { new PlayerDto { PlayerId = 1, PlayerName = "TestPlayer" } };

            _mockPlayerService.Setup(s => s.GetByServer(serverId)).Returns(expectedResult);

            // Act
            var result = _controller.GetByServer(serverId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<PlayerDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void GetByGuild_ReturnsOkResult_WithPlayers()
        {
            // Arrange
            int guildId = 1;
            var expectedResult = new List<PlayerDto> { new PlayerDto { PlayerId = 1, PlayerName = "TestPlayer" } };

            _mockPlayerService.Setup(s => s.GetByGuild(guildId)).Returns(expectedResult);

            // Act
            var result = _controller.GetByGuild(guildId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<PlayerDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void Create_ValidPlayer_ReturnsOkResult_WithCreatedPlayerDto()
        {
            // Arrange
            var playerToCreate = new Player { Playerid = 1, Playername = "NewPlayer" };

            // Act
            var result = _controller.Create(playerToCreate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdPlayerDto = Assert.IsType<PlayerDto>(okResult.Value);
            Assert.Equal(playerToCreate.Playerid, createdPlayerDto.PlayerId);
            Assert.Equal(playerToCreate.Playername, createdPlayerDto.PlayerName);
            
            _mockPlayerService.Verify(s => s.Add(playerToCreate), Times.Once);
        }

        [Fact]
        public void Update_ValidPlayer_ReturnsOkResult_WithUpdatedPlayerDto()
        {
            // Arrange
            int id = 1;
            var playerToUpdate = new Player { Playername = "UpdatedPlayer" };

            // Act
            var result = _controller.Update(id, playerToUpdate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedPlayerDto = Assert.IsType<PlayerDto>(okResult.Value);
            
            Assert.Equal(id, playerToUpdate.Playerid);
            Assert.Equal(id, updatedPlayerDto.PlayerId);
            Assert.Equal(playerToUpdate.Playername, updatedPlayerDto.PlayerName);

            _mockPlayerService.Verify(s => s.Update(playerToUpdate), Times.Once);
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
            _mockPlayerService.Verify(s => s.Delete(id), Times.Once);
        }
    }
}
