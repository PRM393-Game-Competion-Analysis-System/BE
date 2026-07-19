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
    public class ServersControllerTests
    {
        private readonly Mock<IServerService> _mockServerService;
        private readonly ServersController _controller;

        public ServersControllerTests()
        {
            _mockServerService = new Mock<IServerService>();
            _controller = new ServersController(_mockServerService.Object);
        }

        [Fact]
        public void GetList_ReturnsOkResult_WithServers()
        {
            // Arrange
            var parameters = new QueryParameters();
            var expectedResult = new PagedResult<ServerDto>
            {
                Items = new List<ServerDto> { new ServerDto { ServerId = 1, ServerName = "TestServer" } },
                TotalCount = 1
            };

            _mockServerService.Setup(s => s.GetAll(parameters)).Returns(expectedResult);

            // Act
            var result = _controller.GetList(parameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<PagedResult<ServerDto>>(okResult.Value);
            Assert.Equal(1, actualResult.TotalCount);
        }

        [Fact]
        public void Search_ReturnsOkResult_WithMatchingServers()
        {
            // Arrange
            var searchName = "Test";
            var expectedResult = new List<ServerDto> { new ServerDto { ServerId = 1, ServerName = "TestServer" } };

            _mockServerService.Setup(s => s.SearchByName(searchName)).Returns(expectedResult);

            // Act
            var result = _controller.Search(searchName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<ServerDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsOkResult_WithServer()
        {
            // Arrange
            int id = 1;
            var expectedServer = new ServerDto { ServerId = id, ServerName = "TestServer" };

            _mockServerService.Setup(s => s.GetById(id)).Returns(expectedServer);

            // Act
            var result = _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualServer = Assert.IsType<ServerDto>(okResult.Value);
            Assert.Equal(id, actualServer.ServerId);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            int id = 999;
            _mockServerService.Setup(s => s.GetById(id)).Returns((ServerDto)null!);

            // Act
            var result = _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void GetByGame_ReturnsOkResult_WithServers()
        {
            // Arrange
            int gameId = 1;
            var expectedResult = new List<ServerDto> { new ServerDto { ServerId = 1, ServerName = "TestServer" } };

            _mockServerService.Setup(s => s.GetByGame(gameId)).Returns(expectedResult);

            // Act
            var result = _controller.GetByGame(gameId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<ServerDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void Create_ValidServer_ReturnsOkResult_WithCreatedServerDto()
        {
            // Arrange
            var serverToCreate = new Server { Serverid = 1, Servername = "NewServer", Region = "NA", Status = "Online" };

            // Act
            var result = _controller.Create(serverToCreate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdServerDto = Assert.IsType<ServerDto>(okResult.Value);
            Assert.Equal(serverToCreate.Serverid, createdServerDto.ServerId);
            Assert.Equal(serverToCreate.Servername, createdServerDto.ServerName);
            
            _mockServerService.Verify(s => s.Add(serverToCreate), Times.Once);
        }

        [Fact]
        public void Update_ValidServer_ReturnsOkResult_WithUpdatedServerDto()
        {
            // Arrange
            int id = 1;
            var serverToUpdate = new Server { Servername = "UpdatedServer", Region = "EU", Status = "Offline" };

            // Act
            var result = _controller.Update(id, serverToUpdate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedServerDto = Assert.IsType<ServerDto>(okResult.Value);
            
            Assert.Equal(id, serverToUpdate.Serverid);
            Assert.Equal(id, updatedServerDto.ServerId);
            Assert.Equal(serverToUpdate.Servername, updatedServerDto.ServerName);

            _mockServerService.Verify(s => s.Update(serverToUpdate), Times.Once);
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
            _mockServerService.Verify(s => s.Delete(id), Times.Once);
        }
    }
}
