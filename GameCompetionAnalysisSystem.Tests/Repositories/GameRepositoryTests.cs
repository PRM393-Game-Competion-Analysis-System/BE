using System;
using System.Linq;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Repositories
{
    public class GameRepositoryTests
    {
        private PRM393GameAiContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PRM393GameAiContext(options);
        }

        [Fact]
        public void Add_ValidGame_AddsToDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GameRepository(context);
            var game = new Game { Gamename = "VLTK Mobile", Genre = "MMORPG" };

            // Act
            repo.Add(game);

            // Assert
            var result = context.Games.FirstOrDefault(g => g.Gamename == "VLTK Mobile");
            Assert.NotNull(result);
            Assert.Equal("MMORPG", result.Genre);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsGameWithInclude()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GameRepository(context);
            var company = new Company { Companyname = "VNG" };
            var game = new Game { Gamename = "VLTK Mobile", Company = company };
            
            repo.Add(game); // Adds both game and company implicitly

            // Act
            var result = repo.GetById(game.Gameid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("VLTK Mobile", result.Gamename);
            Assert.NotNull(result.Company);
            Assert.Equal("VNG", result.Company.Companyname);
        }

        [Fact]
        public void GetAll_WithSearchAndPagination_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GameRepository(context);
            repo.Add(new Game { Gamename = "VLTK Mobile", Genre = "MMORPG" });
            repo.Add(new Game { Gamename = "VLTK 1", Genre = "MMORPG" });
            repo.Add(new Game { Gamename = "PUBG", Genre = "Shooter" });

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "vltk"
            };

            // Act
            var result = repo.GetAll(parameters, out int totalCount);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetMMORPG_ReturnsOnlyMMORPGGames()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GameRepository(context);
            repo.Add(new Game { Gamename = "VLTK Mobile", Genre = "MMORPG" });
            repo.Add(new Game { Gamename = "PUBG", Genre = "Shooter" });

            // Act
            var result = repo.GetMMORPG();

            // Assert
            Assert.Single(result);
            Assert.Equal("MMORPG", result[0].Genre);
        }

        [Fact(Skip = "EF InMemory database does not support NpgsqlDbFunctionsExtensions.ILike")]
        public void SearchByName_MatchesPattern_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GameRepository(context);
            repo.Add(new Game { Gamename = "VLTK Mobile" });
            repo.Add(new Game { Gamename = "VLTK-1" });
            repo.Add(new Game { Gamename = "PUBG" });

            // Act
            var result = repo.SearchByName("vltk");

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Update_ExistingGame_UpdatesDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GameRepository(context);
            var game = new Game { Gamename = "VLTK Mobile", Genre = "MMORPG" };
            repo.Add(game);

            // Act
            game.Genre = "Action RPG";
            repo.Update(game);

            // Assert
            var result = context.Games.Find(game.Gameid);
            Assert.NotNull(result);
            Assert.Equal("Action RPG", result.Genre);
        }

        [Fact]
        public void Delete_ExistingGame_RemovesFromDatabaseWithRelatedEntities()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new GameRepository(context);
            var game = new Game 
            { 
                Gamename = "VLTK Mobile",
                Events = new System.Collections.Generic.List<Event> 
                { 
                    new Event { Eventname = "Top Phuong" }
                }
            };
            repo.Add(game);

            // Act
            repo.Delete(game.Gameid);

            // Assert
            var result = context.Games.Find(game.Gameid);
            Assert.Null(result);
            Assert.Empty(context.Events); // Related entities should also be deleted
        }
    }
}
