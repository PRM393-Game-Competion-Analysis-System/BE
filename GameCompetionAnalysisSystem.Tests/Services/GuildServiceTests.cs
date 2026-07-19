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
    public class GuildServiceTests
    {
        private readonly Mock<IGuildRepository> _mockRepo;
        private readonly GuildService _service;

        public GuildServiceTests()
        {
            _mockRepo = new Mock<IGuildRepository>();
            _service = new GuildService(_mockRepo.Object);
        }

        private Guild CreateSampleGuild(int id, string name, string serverName)
        {
            return new Guild
            {
                Guildid = id,
                Guildname = name,
                Server = new Server { Servername = serverName }
            };
        }

        [Fact]
        public void GetAll_ReturnsPagedResultOfGuildDto()
        {
            // Arrange
            var parameters = new QueryParameters { PageNumber = 1, PageSize = 10 };
            var guilds = new List<Guild>
            {
                CreateSampleGuild(1, "Guild1", "S1"),
                CreateSampleGuild(2, "Guild2", "S2")
            };
            int expectedTotalCount = 2;

            _mockRepo.Setup(r => r.GetAll(parameters, out expectedTotalCount)).Returns(guilds);

            // Act
            var result = _service.GetAll(parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("Guild1", result.Items.First().GuildName);
            Assert.Equal("S1", result.Items.First().ServerName);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsGuildDto()
        {
            // Arrange
            int id = 1;
            var guild = CreateSampleGuild(id, "Guild1", "S1");
            _mockRepo.Setup(r => r.GetById(id)).Returns(guild);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.GuildId);
            Assert.Equal("Guild1", result.GuildName);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNull()
        {
            // Arrange
            int id = 99;
            _mockRepo.Setup(r => r.GetById(id)).Returns((Guild)null!);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetByServer_ExistingServerId_ReturnsList()
        {
            // Arrange
            int serverId = 1;
            var guilds = new List<Guild> { CreateSampleGuild(1, "Guild1", "S1") };
            _mockRepo.Setup(r => r.GetByServer(serverId)).Returns(guilds);

            // Act
            var result = _service.GetByServer(serverId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Guild1", result.First().GuildName);
        }

        [Fact]
        public void SearchByName_Matches_ReturnsList()
        {
            // Arrange
            string name = "Guild1";
            var guilds = new List<Guild> { CreateSampleGuild(1, "Guild1", "S1") };
            _mockRepo.Setup(r => r.SearchByName(name)).Returns(guilds);

            // Act
            var result = _service.SearchByName(name);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Guild1", result.First().GuildName);
        }

        [Fact]
        public void Add_CallsRepositoryAdd()
        {
            // Arrange
            var guild = new Guild { Guildname = "NewGuild" };

            // Act
            _service.Add(guild);

            // Assert
            _mockRepo.Verify(r => r.Add(guild), Times.Once);
        }

        [Fact]
        public void Update_CallsRepositoryUpdate()
        {
            // Arrange
            var guild = new Guild { Guildid = 1, Guildname = "UpdatedGuild" };

            // Act
            _service.Update(guild);

            // Assert
            _mockRepo.Verify(r => r.Update(guild), Times.Once);
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
