using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BIL.Service;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Services
{
    public class AIAnalysisServiceTests
    {
        private readonly Mock<IAIAnalysisRepository> _mockRepo;
        private readonly AIAnalysisService _service;

        public AIAnalysisServiceTests()
        {
            _mockRepo = new Mock<IAIAnalysisRepository>();
            _service = new AIAnalysisService(_mockRepo.Object);
        }

        private Aianalysis CreateSampleAnalysis()
        {
            return new Aianalysis
            {
                Analysisid = 1,
                Processedtime = DateTime.UtcNow,
                Upload = new Imageupload { Imageurl = "http://example.com/image.png" },
                Aiextractedfields = new List<Aiextractedfield>
                {
                    new Aiextractedfield { Fieldtype = "GameName", Rawtext = "VLTK Mobile", Confidence = 0.9 },
                    new Aiextractedfield { Fieldtype = "ServerName", Rawtext = "S1", Confidence = 0.8 }
                },
                Leaderboards = new List<Leaderboard>
                {
                    new Leaderboard
                    {
                        Event = new Event { Eventname = "Top Phuong" },
                        Leaderboardentries = new List<Leaderboardentry>
                        {
                            new Leaderboardentry
                            {
                                Rank = 1,
                                Value = 1000,
                                Player = new Player { Playername = "Player1", Guild = new Guild { Guildname = "Guild1" } }
                            }
                        }
                    }
                }
            };
        }

        [Fact]
        public async Task AnalyzeScreenshotAsync_ValidFile_ReturnsAnalysisResultDto()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            var analysis = CreateSampleAnalysis();

            _mockRepo.Setup(r => r.ProcessScreenshotAsync(mockFile.Object, 1, "VLTK Mobile"))
                .ReturnsAsync(analysis);
            
            _mockRepo.Setup(r => r.GetByIdWithDetailsAsync(analysis.Analysisid))
                .ReturnsAsync(analysis);

            // Act
            var result = await _service.AnalyzeScreenshotAsync(mockFile.Object, 1, "VLTK Mobile");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.AnalysisId);
            Assert.Equal("VLTK Mobile", result.GameName);
            Assert.Equal("S1", result.ServerName);
            Assert.Equal("Top Phuong", result.EventName);
            Assert.Single(result.Leaderboard);
            Assert.Equal("Player1", result.Leaderboard.First().PlayerName);
        }

        [Fact]
        public async Task AnalyzeScreenshotAsync_RepoReturnsNull_ReturnsNull()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();

            _mockRepo.Setup(r => r.ProcessScreenshotAsync(mockFile.Object, 1, "VLTK Mobile"))
                .ReturnsAsync((Aianalysis)null!);

            // Act
            var result = await _service.AnalyzeScreenshotAsync(mockFile.Object, 1, "VLTK Mobile");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AnalyzeLatestFromCloudAsync_ValidCall_ReturnsAnalysisResultDto()
        {
            // Arrange
            var analysis = CreateSampleAnalysis();

            _mockRepo.Setup(r => r.ProcessLatestImageFromCloudAsync(1, "VLTK 2.0"))
                .ReturnsAsync(analysis);
            
            _mockRepo.Setup(r => r.GetByIdWithDetailsAsync(analysis.Analysisid))
                .ReturnsAsync(analysis);

            // Act
            var result = await _service.AnalyzeLatestFromCloudAsync(1, "VLTK 2.0");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.AnalysisId);
        }

        [Fact]
        public async Task AnalyzeLatestFromCloudAsync_RepoReturnsNull_ReturnsNull()
        {
            // Arrange
            _mockRepo.Setup(r => r.ProcessLatestImageFromCloudAsync(1, "VLTK 2.0"))
                .ReturnsAsync((Aianalysis)null!);

            // Act
            var result = await _service.AnalyzeLatestFromCloudAsync(1, "VLTK 2.0");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetHistoryAsync_AdminRole_FiltersCorrectly()
        {
            // Arrange
            var parameters = new AIQueryParameters();
            var analysisList = new List<Aianalysis> { CreateSampleAnalysis() };
            int totalCount = 1;

            _mockRepo.Setup(r => r.GetAllAsync(parameters, null))
                .ReturnsAsync((analysisList, totalCount));

            // Act
            var result = await _service.GetHistoryAsync(1, "admin", parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal("VLTK Mobile", result.Items.First().GameName);
        }

        [Fact]
        public async Task GetHistoryAsync_UserRole_FiltersByUserId()
        {
            // Arrange
            var parameters = new AIQueryParameters();
            var analysisList = new List<Aianalysis> { CreateSampleAnalysis() };
            int totalCount = 1;

            _mockRepo.Setup(r => r.GetAllAsync(parameters, 2))
                .ReturnsAsync((analysisList, totalCount));

            // Act
            var result = await _service.GetHistoryAsync(2, "user", parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            _mockRepo.Verify(r => r.GetAllAsync(parameters, 2), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            // Arrange
            var analysis = CreateSampleAnalysis();
            _mockRepo.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(analysis);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.AnalysisId);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync((Aianalysis)null!);

            // Act
            var result = await _service.GetByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_ReturnsTrue()
        {
            // Arrange
            _mockRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetAirtestUploadImagesAsync_ReturnsStringList()
        {
            // Arrange
            var expectedList = new List<string> { "url1", "url2" };
            _mockRepo.Setup(r => r.GetAirtestUploadImagesAsync()).ReturnsAsync(expectedList);

            // Act
            var result = await _service.GetAirtestUploadImagesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("url1", result.First());
        }

        [Fact]
        public async Task GetHeatmapDataAsync_AdminRole_PassesNullUserId()
        {
            // Arrange
            var expectedData = new List<HeatmapDto> { new HeatmapDto() };
            _mockRepo.Setup(r => r.GetHeatmapDataAsync(null)).ReturnsAsync(expectedData);

            // Act
            var result = await _service.GetHeatmapDataAsync(1, "admin");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            _mockRepo.Verify(r => r.GetHeatmapDataAsync(null), Times.Once);
        }
    }
}
