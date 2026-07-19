using System.Collections.Generic;
using System.Threading.Tasks;
using BIL.Service;
using DAL.DTO;
using GameCompetionAnalysisSystem.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Controllers
{
    public class LeaderboardControllerTests
    {
        private readonly Mock<ILeaderboardService> _mockLeaderboardService;
        private readonly LeaderboardController _controller;

        public LeaderboardControllerTests()
        {
            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [Fact]
        public async Task ParseOcr_ReturnsOkResult()
        {
            // Arrange
            int analysisId = 1;
            _mockLeaderboardService.Setup(s => s.ProcessOcrAsync(analysisId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ParseOcr(analysisId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockLeaderboardService.Verify(s => s.ProcessOcrAsync(analysisId), Times.Once);
        }

        [Fact]
        public async Task GetTop_ReturnsOkResult_WithEntries()
        {
            // Arrange
            int n = 10;
            var expectedResult = new List<LeaderboardEntryDto> { new LeaderboardEntryDto { Rank = 1 } };
            _mockLeaderboardService.Setup(s => s.GetTopAsync(n)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetTop(n);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithLeaderboards()
        {
            // Arrange
            var parameters = new QueryParameters();
            var expectedResult = new PagedResult<LeaderboardDto>
            {
                Items = new List<LeaderboardDto> { new LeaderboardDto { LeaderboardId = 1 } },
                TotalCount = 1
            };
            _mockLeaderboardService.Setup(s => s.GetAllAsync(parameters)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(parameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<PagedResult<LeaderboardDto>>(okResult.Value);
            Assert.Equal(1, actualResult.TotalCount);
        }

        [Fact]
        public async Task GetById_ExistingId_ReturnsOkResult_WithLeaderboard()
        {
            // Arrange
            int id = 1;
            var expectedLeaderboard = new LeaderboardDto { LeaderboardId = id };
            _mockLeaderboardService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(expectedLeaderboard);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualLeaderboard = Assert.IsType<LeaderboardDto>(okResult.Value);
            Assert.Equal(id, actualLeaderboard.LeaderboardId);
        }

        [Fact]
        public async Task GetById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            int id = 999;
            _mockLeaderboardService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((LeaderboardDto)null!);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetEntries_ReturnsOkResult_WithEntries()
        {
            // Arrange
            int id = 1;
            var expectedResult = new List<LeaderboardEntryDto> { new LeaderboardEntryDto { Rank = 1 } };
            _mockLeaderboardService.Setup(s => s.GetEntriesByLeaderboardIdAsync(id)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetEntries(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<LeaderboardEntryDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public async Task GetSortedEntries_ReturnsOkResult_WithSortedEntries()
        {
            // Arrange
            int id = 1;
            var expectedResult = new List<LeaderboardEntryDto> { new LeaderboardEntryDto { Rank = 1 } };
            _mockLeaderboardService.Setup(s => s.GetSortedEntriesByLeaderboardIdAsync(id)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetSortedEntries(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<LeaderboardEntryDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public async Task Delete_ReturnsOkResult()
        {
            // Arrange
            int id = 1;
            _mockLeaderboardService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockLeaderboardService.Verify(s => s.DeleteAsync(id), Times.Once);
        }
    }
}
