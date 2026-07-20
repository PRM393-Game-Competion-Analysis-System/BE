using System;
using System.Linq;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Repositories
{
    public class EventRepositoryTests
    {
        private PRM393GameAiContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PRM393GameAiContext(options);
        }

        [Fact]
        public void Add_ValidEvent_AddsToDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new EventRepository(context);
            var @event = new Event { Eventname = "Top Phuong", Eventtype = "Ranking" };

            // Act
            repo.Add(@event);

            // Assert
            var result = context.Events.FirstOrDefault(e => e.Eventname == "Top Phuong");
            Assert.NotNull(result);
            Assert.Equal("Ranking", result.Eventtype);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsEventWithInclude()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new EventRepository(context);
            var game = new Game { Gamename = "VLTK Mobile" };
            var @event = new Event { Eventname = "Top Phuong", Game = game };
            
            repo.Add(@event);

            // Act
            var result = repo.GetById(@event.Eventid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Top Phuong", result.Eventname);
            Assert.NotNull(result.Game);
            Assert.Equal("VLTK Mobile", result.Game.Gamename);
        }

        [Fact]
        public void GetAll_WithSearchAndPagination_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new EventRepository(context);
            repo.Add(new Event { Eventname = "Top Phuong", Eventtype = "Ranking" });
            repo.Add(new Event { Eventname = "Top Bang", Eventtype = "Ranking" });
            repo.Add(new Event { Eventname = "PK", Eventtype = "Tournament" });

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "top"
            };

            // Act
            var result = repo.GetAll(parameters, out int totalCount);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetByGame_ExistingGameId_ReturnsEvents()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new EventRepository(context);
            var game = new Game { Gamename = "Game 1" };
            repo.Add(new Event { Eventname = "Event1", Game = game });

            // Act
            var result = repo.GetByGame(game.Gameid);

            // Assert
            Assert.Single(result);
            Assert.Equal("Event1", result[0].Eventname);
        }

        [Fact(Skip = "EF InMemory database does not support NpgsqlDbFunctionsExtensions.ILike")]
        public void SearchByName_MatchesPattern_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new EventRepository(context);
            repo.Add(new Event { Eventname = "Top Phuong" });

            // Act
            var result = repo.SearchByName("top");

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void Update_ExistingEvent_UpdatesDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new EventRepository(context);
            var @event = new Event { Eventname = "Top Phuong", Eventtype = "Ranking" };
            repo.Add(@event);

            // Act
            @event.Eventtype = "Updated";
            repo.Update(@event);

            // Assert
            var result = context.Events.Find(@event.Eventid);
            Assert.NotNull(result);
            Assert.Equal("Updated", result.Eventtype);
        }

        [Fact]
        public void Delete_ExistingEvent_RemovesFromDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new EventRepository(context);
            var @event = new Event { Eventname = "Top Phuong" };
            repo.Add(@event);

            // Act
            repo.Delete(@event.Eventid);

            // Assert
            var result = context.Events.Find(@event.Eventid);
            Assert.Null(result);
        }
    }
}
