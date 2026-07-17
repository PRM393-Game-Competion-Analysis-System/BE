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
    public class GamesControllerTests
    {
        private readonly Mock<IGameService> _mockGameService;
        private readonly GamesController _controller;

        public GamesControllerTests()
        {
            _mockGameService = new Mock<IGameService>();
            _controller = new GamesController(_mockGameService.Object);
        }

        [Fact]
        public void GetList_ReturnsOkResult_WithGames()
        {
            // Arrange
            var parameters = new QueryParameters();
            var expectedResult = new PagedResult<GameDto>
            {
                Items = new List<GameDto> { new GameDto { GameId = 1, GameName = "TestGame" } },
                TotalCount = 1
            };

            _mockGameService.Setup(s => s.GetAllGames(parameters)).Returns(expectedResult);

            // Act
            var result = _controller.GetList(parameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<PagedResult<GameDto>>(okResult.Value);
            Assert.Equal(1, actualResult.TotalCount);
        }

        [Fact]
        public void Search_ReturnsOkResult_WithMatchingGames()
        {
            // Arrange
            var searchName = "Test";
            var expectedResult = new List<GameDto> { new GameDto { GameId = 1, GameName = "TestGame" } };

            _mockGameService.Setup(s => s.SearchByName(searchName)).Returns(expectedResult);

            // Act
            var result = _controller.Search(searchName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<GameDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsOkResult_WithGame()
        {
            // Arrange
            int id = 1;
            var expectedGame = new GameDto { GameId = id, GameName = "TestGame" };

            _mockGameService.Setup(s => s.GetById(id)).Returns(expectedGame);

            // Act
            var result = _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualGame = Assert.IsType<GameDto>(okResult.Value);
            Assert.Equal(id, actualGame.GameId);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            int id = 999;
            _mockGameService.Setup(s => s.GetById(id)).Returns((GameDto)null!);

            // Act
            var result = _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Create_ValidGame_ReturnsOkResult_WithCreatedGameDto()
        {
            // Arrange
            var gameToCreate = new Game { Gameid = 1, Gamename = "NewGame", Genre = "Action" };

            // Act
            var result = _controller.Create(gameToCreate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdGameDto = Assert.IsType<GameDto>(okResult.Value);
            Assert.Equal(gameToCreate.Gameid, createdGameDto.GameId);
            Assert.Equal(gameToCreate.Gamename, createdGameDto.GameName);
            
            _mockGameService.Verify(s => s.Add(gameToCreate), Times.Once);
        }

        [Fact]
        public void GetMMORPG_ReturnsOkResult_WithGames()
        {
            // Arrange
            var expectedResult = new List<GameDto> { new GameDto { GameId = 1, Genre = "MMORPG" } };
            _mockGameService.Setup(s => s.GetMMORPGGames()).Returns(expectedResult);

            // Act
            var result = _controller.GetMMORPG();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<GameDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void Update_ValidGame_ReturnsOkResult_WithUpdatedGameDto()
        {
            // Arrange
            int id = 1;
            var gameToUpdate = new Game { Gamename = "UpdatedName", Genre = "RPG" };

            // Act
            var result = _controller.Update(id, gameToUpdate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedGameDto = Assert.IsType<GameDto>(okResult.Value);
            
            Assert.Equal(id, gameToUpdate.Gameid);
            Assert.Equal(id, updatedGameDto.GameId);
            Assert.Equal(gameToUpdate.Gamename, updatedGameDto.GameName);

            _mockGameService.Verify(s => s.Update(gameToUpdate), Times.Once);
        }

        [Fact]
        public void Delete_ExistingGame_ReturnsOkResult()
        {
            // Arrange
            int id = 1;
            var existingGame = new GameDto { GameId = id, GameName = "GameToDelete" };
            _mockGameService.Setup(s => s.GetById(id)).Returns(existingGame);

            // Act
            var result = _controller.Delete(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockGameService.Verify(s => s.Delete(id), Times.Once);
        }

        [Fact]
        public void Delete_NonExistingGame_ReturnsNotFoundResult()
        {
            // Arrange
            int id = 999;
            _mockGameService.Setup(s => s.GetById(id)).Returns((GameDto)null!);

            // Act
            var result = _controller.Delete(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockGameService.Verify(s => s.Delete(It.IsAny<int>()), Times.Never);
        }
    }
}
