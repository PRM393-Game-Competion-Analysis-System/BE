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
    public class PlayerServiceTests
    {
        private readonly Mock<IPlayerRepository> _mockRepo;
        private readonly PlayerService _service;

        public PlayerServiceTests()
        {
            _mockRepo = new Mock<IPlayerRepository>();
            _service = new PlayerService(_mockRepo.Object);
        }

        private Player CreateSamplePlayer(int id, string name)
        {
            return new Player
            {
                Playerid = id,
                Playername = name,
                Guild = new Guild { Guildname = "Guild1" },
                Game = new Game { Gamename = "Game1" },
                Server = new Server { Servername = "S1" },
                Leaderboardentries = new List<Leaderboardentry>
                {
                    new Leaderboardentry { Entryid = 1, Rank = 1, Value = 1000 }
                }
            };
        }

        [Fact]
        public void GetAll_ReturnsPagedResultOfPlayerDto()
        {
            // Arrange
            var parameters = new QueryParameters { PageNumber = 1, PageSize = 10 };
            var players = new List<Player>
            {
                CreateSamplePlayer(1, "Player1"),
                CreateSamplePlayer(2, "Player2")
            };
            int expectedTotalCount = 2;

            _mockRepo.Setup(r => r.GetAll(parameters, out expectedTotalCount)).Returns(players);

            // Act
            var result = _service.GetAll(parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("Player1", result.Items.First().PlayerName);
            Assert.Equal("Guild1", result.Items.First().GuildName);
            Assert.Equal(1000, result.Items.First().LatestScore);
            Assert.Equal(1, result.Items.First().LatestRank);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsPlayerDto()
        {
            // Arrange
            int id = 1;
            var player = CreateSamplePlayer(id, "Player1");
            _mockRepo.Setup(r => r.GetById(id)).Returns(player);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.PlayerId);
            Assert.Equal("Player1", result.PlayerName);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNull()
        {
            // Arrange
            int id = 99;
            _mockRepo.Setup(r => r.GetById(id)).Returns((Player)null!);

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
            var players = new List<Player> { CreateSamplePlayer(1, "Player1") };
            _mockRepo.Setup(r => r.GetByGame(gameId)).Returns(players);

            // Act
            var result = _service.GetByGame(gameId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Player1", result.First().PlayerName);
        }

        [Fact]
        public void GetByServer_ExistingServerId_ReturnsList()
        {
            // Arrange
            int serverId = 1;
            var players = new List<Player> { CreateSamplePlayer(1, "Player1") };
            _mockRepo.Setup(r => r.GetByServer(serverId)).Returns(players);

            // Act
            var result = _service.GetByServer(serverId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Player1", result.First().PlayerName);
        }

        [Fact]
        public void GetByGuild_ExistingGuildId_ReturnsList()
        {
            // Arrange
            int guildId = 1;
            var players = new List<Player> { CreateSamplePlayer(1, "Player1") };
            _mockRepo.Setup(r => r.GetByGuild(guildId)).Returns(players);

            // Act
            var result = _service.GetByGuild(guildId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Player1", result.First().PlayerName);
        }

        [Fact]
        public void SearchByName_Matches_ReturnsList()
        {
            // Arrange
            string name = "Player1";
            var players = new List<Player> { CreateSamplePlayer(1, "Player1") };
            _mockRepo.Setup(r => r.SearchByName(name)).Returns(players);

            // Act
            var result = _service.SearchByName(name);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Player1", result.First().PlayerName);
        }

        [Fact]
        public void Add_CallsRepositoryAdd()
        {
            // Arrange
            var player = new Player { Playername = "NewPlayer" };

            // Act
            _service.Add(player);

            // Assert
            _mockRepo.Verify(r => r.Add(player), Times.Once);
        }

        [Fact]
        public void Update_CallsRepositoryUpdate()
        {
            // Arrange
            var player = new Player { Playerid = 1, Playername = "UpdatedPlayer" };

            // Act
            _service.Update(player);

            // Assert
            _mockRepo.Verify(r => r.Update(player), Times.Once);
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
