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
    public class ServerServiceTests
    {
        private readonly Mock<IServerRepository> _mockRepo;
        private readonly ServerService _service;

        public ServerServiceTests()
        {
            _mockRepo = new Mock<IServerRepository>();
            _service = new ServerService(_mockRepo.Object);
        }

        private Server CreateSampleServer(int id, string name)
        {
            return new Server
            {
                Serverid = id,
                Servername = name,
                Region = "VN",
                Status = "Active",
                Game = new Game { Gamename = "Game1" }
            };
        }

        [Fact]
        public void GetAll_ReturnsPagedResultOfServerDto()
        {
            // Arrange
            var parameters = new QueryParameters { PageNumber = 1, PageSize = 10 };
            var servers = new List<Server>
            {
                CreateSampleServer(1, "Server1"),
                CreateSampleServer(2, "Server2")
            };
            int expectedTotalCount = 2;

            _mockRepo.Setup(r => r.GetAll(parameters, out expectedTotalCount)).Returns(servers);

            // Act
            var result = _service.GetAll(parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("Server1", result.Items.First().ServerName);
            Assert.Equal("Game1", result.Items.First().GameName);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsServerDto()
        {
            // Arrange
            int id = 1;
            var server = CreateSampleServer(id, "Server1");
            _mockRepo.Setup(r => r.GetById(id)).Returns(server);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.ServerId);
            Assert.Equal("Server1", result.ServerName);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNull()
        {
            // Arrange
            int id = 99;
            _mockRepo.Setup(r => r.GetById(id)).Returns((Server)null!);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetByGame_ExistingGameId_ReturnsList()
        {
            // Arrange
            int gameId = 1;
            var servers = new List<Server> { CreateSampleServer(1, "Server1") };
            _mockRepo.Setup(r => r.GetByGame(gameId)).Returns(servers);

            // Act
            var result = _service.GetByGame(gameId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Server1", result.First().ServerName);
        }

        [Fact]
        public void SearchByName_Matches_ReturnsList()
        {
            // Arrange
            string name = "Server1";
            var servers = new List<Server> { CreateSampleServer(1, "Server1") };
            _mockRepo.Setup(r => r.SearchByName(name)).Returns(servers);

            // Act
            var result = _service.SearchByName(name);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Server1", result.First().ServerName);
        }

        [Fact]
        public void Add_CallsRepositoryAdd()
        {
            // Arrange
            var server = new Server { Servername = "NewServer" };

            // Act
            _service.Add(server);

            // Assert
            _mockRepo.Verify(r => r.Add(server), Times.Once);
        }

        [Fact]
        public void Update_CallsRepositoryUpdate()
        {
            // Arrange
            var server = new Server { Serverid = 1, Servername = "UpdatedServer" };

            // Act
            _service.Update(server);

            // Assert
            _mockRepo.Verify(r => r.Update(server), Times.Once);
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
    }
}
