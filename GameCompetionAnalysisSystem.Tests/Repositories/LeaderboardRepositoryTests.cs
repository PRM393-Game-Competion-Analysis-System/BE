using System;
using System.Linq;
using System.Threading.Tasks;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Repositories
{
    public class LeaderboardRepositoryTests
    {
        private PRM393GameAiContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PRM393GameAiContext(options);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsLeaderboardsAndCount()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new LeaderboardRepository(context);
            context.Leaderboards.Add(new Leaderboard { Title = "LB1", Metrictype = "Score" });
            context.Leaderboards.Add(new Leaderboard { Title = "LB2", Metrictype = "Rank" });
            await context.SaveChangesAsync();

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "LB"
            };

            // Act
            var (items, totalCount) = await repo.GetAllAsync(parameters);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsLeaderboard()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new LeaderboardRepository(context);
            var lb = new Leaderboard { Title = "LB1", Metrictype = "Score" };
            context.Leaderboards.Add(lb);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetByIdAsync(lb.Leaderboardid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("LB1", result.Title);
        }

        [Fact]
        public async Task GetEntriesByLeaderboardIdAsync_ReturnsEntriesInOrder()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new LeaderboardRepository(context);
            
            var lb = new Leaderboard { Title = "LB1" };
            var player1 = new Player { Playername = "P1" };
            var player2 = new Player { Playername = "P2" };

            context.Leaderboards.Add(lb);
            context.Players.AddRange(player1, player2);
            await context.SaveChangesAsync();

            context.Leaderboardentries.Add(new Leaderboardentry { Leaderboardid = lb.Leaderboardid, Playerid = player1.Playerid, Rank = 2, Value = 100 });
            context.Leaderboardentries.Add(new Leaderboardentry { Leaderboardid = lb.Leaderboardid, Playerid = player2.Playerid, Rank = 1, Value = 200 });
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetEntriesByLeaderboardIdAsync(lb.Leaderboardid);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Rank); // Should be ordered by Rank ascending
            Assert.Equal(2, result[1].Rank);
        }

        [Fact]
        public async Task GetSortedEntriesByLeaderboardIdAsync_ReturnsEntriesOrderedByValueThenRank()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new LeaderboardRepository(context);
            
            var lb = new Leaderboard { Title = "LB1" };
            var player1 = new Player { Playername = "P1" };
            var player2 = new Player { Playername = "P2" };
            var player3 = new Player { Playername = "P3" };

            context.Leaderboards.Add(lb);
            context.Players.AddRange(player1, player2, player3);
            await context.SaveChangesAsync();

            context.Leaderboardentries.Add(new Leaderboardentry { Leaderboardid = lb.Leaderboardid, Playerid = player1.Playerid, Rank = 2, Value = 100 });
            context.Leaderboardentries.Add(new Leaderboardentry { Leaderboardid = lb.Leaderboardid, Playerid = player2.Playerid, Rank = 1, Value = 200 });
            context.Leaderboardentries.Add(new Leaderboardentry { Leaderboardid = lb.Leaderboardid, Playerid = player3.Playerid, Rank = 3, Value = 100 });
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetSortedEntriesByLeaderboardIdAsync(lb.Leaderboardid);

            // Assert
            Assert.Equal(3, result.Count);
            // Expected order: Value DESC, Rank ASC
            Assert.Equal(200, result[0].Value); 
            Assert.Equal(100, result[1].Value);
            Assert.Equal(2, result[1].Rank);
            Assert.Equal(100, result[2].Value);
            Assert.Equal(3, result[2].Rank);
        }

        [Fact]
        public async Task DeleteAsync_ExistingLeaderboard_RemovesFromDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new LeaderboardRepository(context);
            var lb = new Leaderboard { Title = "LB1" };
            context.Leaderboards.Add(lb);
            await context.SaveChangesAsync();

            // Act
            await repo.DeleteAsync(lb.Leaderboardid);

            // Assert
            var result = await context.Leaderboards.FindAsync(lb.Leaderboardid);
            Assert.Null(result);
        }
    }
}
