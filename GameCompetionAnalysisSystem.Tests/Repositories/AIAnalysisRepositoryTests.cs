using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Repositories
{
    public class AIAnalysisRepositoryTests
    {
        private PRM393GameAiContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PRM393GameAiContext(options);
        }

        private AIAnalysisRepository CreateRepository(PRM393GameAiContext context)
        {
            var httpClient = new HttpClient();
            var configMock = new Mock<IConfiguration>();
            
            // Mock minimum Cloudinary config to prevent constructor exception
            configMock.Setup(c => c["Cloudinary:CloudName"]).Returns("mockName");
            configMock.Setup(c => c["Cloudinary:ApiKey"]).Returns("mockKey");
            configMock.Setup(c => c["Cloudinary:ApiSecret"]).Returns("mockSecret");

            return new AIAnalysisRepository(context, httpClient, configMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAnalysesAndCount()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = CreateRepository(context);
            
            var user = new User { Username = "TestUser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var upload = new Imageupload { Userid = user.Userid, Imageurl = "url1", Status = "Success" };
            context.Imageuploads.Add(upload);
            await context.SaveChangesAsync();

            context.Aianalyses.Add(new Aianalysis { Uploadid = upload.Uploadid, Aimodelversion = "v1" });
            context.Aianalyses.Add(new Aianalysis { Uploadid = upload.Uploadid, Aimodelversion = "v2" });
            await context.SaveChangesAsync();

            var parameters = new AIQueryParameters
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var (items, totalCount) = await repo.GetAllAsync(parameters, user.Userid);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsAnalysis()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = CreateRepository(context);
            var analysis = new Aianalysis { Aimodelversion = "v1" };
            context.Aianalyses.Add(analysis);
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetByIdAsync(analysis.Analysisid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("v1", result.Aimodelversion);
        }

        [Fact]
        public async Task DeleteAsync_ExistingAnalysis_RemovesFromDatabaseWithRelatedEntities()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = CreateRepository(context);
            var upload = new Imageupload { Imageurl = "url", Status = "Success" };
            var analysis = new Aianalysis 
            { 
                Upload = upload,
                Aiextractedfields = new List<Aiextractedfield> { new Aiextractedfield { Fieldtype = "Test" } },
                Leaderboards = new List<Leaderboard> 
                { 
                    new Leaderboard { Title = "LB1", Leaderboardentries = new List<Leaderboardentry> { new Leaderboardentry { Rank = 1, Value = 100 } } } 
                }
            };
            
            context.Aianalyses.Add(analysis);
            await context.SaveChangesAsync();

            // Act
            var isDeleted = await repo.DeleteAsync(analysis.Analysisid);

            // Assert
            Assert.True(isDeleted);
            Assert.Null(await context.Aianalyses.FindAsync(analysis.Analysisid));
            Assert.Empty(context.Aiextractedfields);
            Assert.Empty(context.Leaderboards);
            Assert.Empty(context.Leaderboardentries);
            Assert.Empty(context.Imageuploads);
        }

        [Fact]
        public async Task GetHeatmapDataAsync_ReturnsCorrectTrendData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = CreateRepository(context);
            
            var user = new User { Username = "TestUser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var upload = new Imageupload { Userid = user.Userid, Imageurl = "url", Status = "Success" };
            context.Imageuploads.Add(upload);
            
            var baseDate = new DateTime(2023, 1, 1);
            var analysis1 = new Aianalysis { Upload = upload, Processedtime = baseDate };
            var analysis2 = new Aianalysis { Upload = upload, Processedtime = baseDate.AddDays(1) };
            context.Aianalyses.AddRange(analysis1, analysis2);

            var lb1 = new Leaderboard { Createdfromanalysis = analysis1 };
            var lb2 = new Leaderboard { Createdfromanalysis = analysis2 };
            context.Leaderboards.AddRange(lb1, lb2);

            var player = new Player { Playername = "TestPlayer" };
            context.Players.Add(player);
            await context.SaveChangesAsync();

            // First entry: Score = 100
            context.Leaderboardentries.Add(new Leaderboardentry { Leaderboardid = lb1.Leaderboardid, Playerid = player.Playerid, Value = 100 });
            // Second entry: Score = 150 (Increased! This should be counted in heatmap)
            context.Leaderboardentries.Add(new Leaderboardentry { Leaderboardid = lb2.Leaderboardid, Playerid = player.Playerid, Value = 150 });
            await context.SaveChangesAsync();

            // Act
            var result = await repo.GetHeatmapDataAsync(user.Userid);

            // Assert
            Assert.NotEmpty(result);
            var dayData = result.FirstOrDefault(d => d.Date == baseDate.AddDays(1).ToString("yyyy-MM-dd")); // Day 2 has an increase
            Assert.NotNull(dayData);
            Assert.Single(dayData.Players); // TestPlayer's score increased
            Assert.Equal("TestPlayer", dayData.Players[0].PlayerName);
        }
    }
}
