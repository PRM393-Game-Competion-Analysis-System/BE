using System;
using System.Linq;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Repositories
{
    public class PlayerRepositoryTests
    {
        private PRM393GameAiContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PRM393GameAiContext(options);
        }

        [Fact]
        public void Add_ValidPlayer_AddsToDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PlayerRepository(context);
            var player = new Player { Playername = "Player1" };

            // Act
            repo.Add(player);

            // Assert
            var result = context.Players.FirstOrDefault(p => p.Playername == "Player1");
            Assert.NotNull(result);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsPlayerWithInclude()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PlayerRepository(context);
            var game = new Game { Gamename = "Game 1" };
            var player = new Player { Playername = "Player1", Game = game };
            repo.Add(player);

            // Act
            var result = repo.GetById(player.Playerid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Player1", result.Playername);
            Assert.NotNull(result.Game);
            Assert.Equal("Game 1", result.Game.Gamename);
        }

        [Fact]
        public void GetAll_WithSearchAndFilter_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PlayerRepository(context);
            
            var game1 = new Game { Gameid = 1, Gamename = "Game1" };
            var game2 = new Game { Gameid = 2, Gamename = "Game2" };

            repo.Add(new Player { Playername = "Player1", Game = game1 });
            repo.Add(new Player { Playername = "Player2", Game = game1 });
            repo.Add(new Player { Playername = "Player3", Game = game2 });

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                Filter = "1", // GameId = 1
                SearchTerm = "player"
            };

            // Act
            var result = repo.GetAll(parameters, out int totalCount);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal(1, p.Gameid));
        }

        [Fact]
        public void GetByGame_ExistingGameId_ReturnsPlayers()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PlayerRepository(context);
            var game = new Game { Gameid = 1, Gamename = "Game1" };
            repo.Add(new Player { Playername = "Player1", Game = game });

            // Act
            var result = repo.GetByGame(game.Gameid);

            // Assert
            Assert.Single(result);
            Assert.Equal("Player1", result[0].Playername);
        }

        [Fact]
        public void GetByServer_ExistingServerId_ReturnsPlayers()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PlayerRepository(context);
            var server = new Server { Serverid = 1, Servername = "S1" };
            repo.Add(new Player { Playername = "Player1", Server = server });

            // Act
            var result = repo.GetByServer(server.Serverid);

            // Assert
            Assert.Single(result);
            Assert.Equal("Player1", result[0].Playername);
        }

        [Fact]
        public void GetByGuild_ExistingGuildId_ReturnsPlayers()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PlayerRepository(context);
            var guild = new Guild { Guildid = 1, Guildname = "G1" };
            repo.Add(new Player { Playername = "Player1", Guild = guild });

            // Act
            var result = repo.GetByGuild(guild.Guildid);

            // Assert
            Assert.Single(result);
            Assert.Equal("Player1", result[0].Playername);
        }

        [Fact(Skip = "EF InMemory database does not support NpgsqlDbFunctionsExtensions.ILike")]
        public void SearchByName_MatchesPattern_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PlayerRepository(context);
            repo.Add(new Player { Playername = "Player1" });

            // Act
            var result = repo.SearchByName("player1");

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void Update_ExistingPlayer_UpdatesDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PlayerRepository(context);
            var player = new Player { Playername = "Player1" };
            repo.Add(player);

            // Act
            player.Playername = "UpdatedPlayer";
            repo.Update(player);

            // Assert
            var result = context.Players.Find(player.Playerid);
            Assert.NotNull(result);
            Assert.Equal("UpdatedPlayer", result.Playername);
        }

        [Fact]
        public void Delete_ExistingPlayer_RemovesFromDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new PlayerRepository(context);
            var player = new Player { Playername = "Player1" };
            repo.Add(player);

            // Act
            repo.Delete(player.Playerid);

            // Assert
            var result = context.Players.Find(player.Playerid);
            Assert.Null(result);
        }
    }
}
