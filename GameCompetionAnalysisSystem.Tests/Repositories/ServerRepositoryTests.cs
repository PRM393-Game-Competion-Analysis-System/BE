using System;
using System.Linq;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Repositories
{
    public class ServerRepositoryTests
    {
        private PRM393GameAiContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PRM393GameAiContext(options);
        }

        [Fact]
        public void Add_ValidServer_AddsToDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new ServerRepository(context);
            var server = new Server { Servername = "S1", Region = "VN" };

            // Act
            repo.Add(server);

            // Assert
            var result = context.Servers.FirstOrDefault(s => s.Servername == "S1");
            Assert.NotNull(result);
            Assert.Equal("VN", result.Region);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsServerWithInclude()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new ServerRepository(context);
            var game = new Game { Gamename = "Game 1" };
            var server = new Server { Servername = "S1", Game = game };
            repo.Add(server);

            // Act
            var result = repo.GetById(server.Serverid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("S1", result.Servername);
            Assert.NotNull(result.Game);
            Assert.Equal("Game 1", result.Game.Gamename);
        }

        [Fact]
        public void GetAll_WithSearchAndPagination_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new ServerRepository(context);
            repo.Add(new Server { Servername = "S1", Region = "VN", Status = "Active" });
            repo.Add(new Server { Servername = "S2", Region = "US", Status = "Active" });
            repo.Add(new Server { Servername = "Global", Region = "VN", Status = "Inactive" });

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "vn"
            };

            // Act
            var result = repo.GetAll(parameters, out int totalCount);

            // Assert
            Assert.Equal(2, totalCount); // Matches Region "VN"
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetByGame_ExistingGameId_ReturnsServers()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new ServerRepository(context);
            var game = new Game { Gamename = "Game 1" };
            repo.Add(new Server { Servername = "S1", Game = game });

            // Act
            var result = repo.GetByGame(game.Gameid);

            // Assert
            Assert.Single(result);
            Assert.Equal("S1", result[0].Servername);
        }

        [Fact(Skip = "EF InMemory database does not support NpgsqlDbFunctionsExtensions.ILike")]
        public void SearchByName_MatchesPattern_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new ServerRepository(context);
            repo.Add(new Server { Servername = "S1" });

            // Act
            var result = repo.SearchByName("s1");

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void Update_ExistingServer_UpdatesDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new ServerRepository(context);
            var server = new Server { Servername = "S1", Region = "VN" };
            repo.Add(server);

            // Act
            server.Region = "Global";
            repo.Update(server);

            // Assert
            var result = context.Servers.Find(server.Serverid);
            Assert.NotNull(result);
            Assert.Equal("Global", result.Region);
        }

        [Fact]
        public void Delete_ExistingServer_RemovesFromDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new ServerRepository(context);
            var server = new Server { Servername = "S1" };
            repo.Add(server);

            // Act
            repo.Delete(server.Serverid);

            // Assert
            var result = context.Servers.Find(server.Serverid);
            Assert.Null(result);
        }
    }
}
