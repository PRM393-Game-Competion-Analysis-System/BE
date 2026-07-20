using System;
using System.Linq;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Repositories
{
    public class GuildRepositoryTests
    {
        private PRM393GameAiContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PRM393GameAiContext(options);
        }

        [Fact]
        public void Add_ValidGuild_AddsToDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GuildRepository(context);
            var guild = new Guild { Guildname = "Guild1" };

            // Act
            repo.Add(guild);

            // Assert
            var result = context.Guilds.FirstOrDefault(g => g.Guildname == "Guild1");
            Assert.NotNull(result);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsGuildWithInclude()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GuildRepository(context);
            var server = new Server { Servername = "S1" };
            var guild = new Guild { Guildname = "Guild1", Server = server };
            repo.Add(guild);

            // Act
            var result = repo.GetById(guild.Guildid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Guild1", result.Guildname);
            Assert.NotNull(result.Server);
            Assert.Equal("S1", result.Server.Servername);
        }

        [Fact]
        public void GetAll_WithSearchAndFilter_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GuildRepository(context);
            
            var server1 = new Server { Serverid = 1, Servername = "S1" };
            var server2 = new Server { Serverid = 2, Servername = "S2" };

            repo.Add(new Guild { Guildname = "Guild1", Server = server1 });
            repo.Add(new Guild { Guildname = "Guild2", Server = server1 });
            repo.Add(new Guild { Guildname = "Guild3", Server = server2 });

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                Filter = "1", // ServerId = 1
                SearchTerm = "guild"
            };

            // Act
            var result = repo.GetAll(parameters, out int totalCount);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Equal(2, result.Count);
            Assert.All(result, g => Assert.Equal(1, g.Serverid));
        }

        [Fact]
        public void GetByServer_ExistingServerId_ReturnsGuilds()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GuildRepository(context);
            var server = new Server { Serverid = 1, Servername = "S1" };
            repo.Add(new Guild { Guildname = "Guild1", Server = server });

            // Act
            var result = repo.GetByServer(server.Serverid);

            // Assert
            Assert.Single(result);
            Assert.Equal("Guild1", result[0].Guildname);
        }

        [Fact(Skip = "EF InMemory database does not support NpgsqlDbFunctionsExtensions.ILike")]
        public void SearchByName_MatchesPattern_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GuildRepository(context);
            repo.Add(new Guild { Guildname = "Guild1" });

            // Act
            var result = repo.SearchByName("guild1");

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void Update_ExistingGuild_UpdatesDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GuildRepository(context);
            var guild = new Guild { Guildname = "Guild1" };
            repo.Add(guild);

            // Act
            guild.Guildname = "UpdatedGuild";
            repo.Update(guild);

            // Assert
            var result = context.Guilds.Find(guild.Guildid);
            Assert.NotNull(result);
            Assert.Equal("UpdatedGuild", result.Guildname);
        }

        [Fact]
        public void Delete_ExistingGuild_RemovesFromDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GuildRepository(context);
            var guild = new Guild { Guildname = "Guild1" };
            repo.Add(guild);

            // Act
            repo.Delete(guild.Guildid);

            // Assert
            var result = context.Guilds.Find(guild.Guildid);
            Assert.Null(result);
        }
    }
}
