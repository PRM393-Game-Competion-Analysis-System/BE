using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BIL.Service;
using DAL.DTO;
using GameCompetionAnalysisSystem.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Controllers
{
    public class AIControllerTests
    {
        private readonly Mock<IAIAnalysisService> _mockAiService;
        private readonly AIController _controller;

        public AIControllerTests()
        {
            _mockAiService = new Mock<IAIAnalysisService>();
            _controller = new AIController(_mockAiService.Object);
        }

        private void SetUserContext(string userId, string role = "user")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("UserId", userId),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        private void SetEmptyUserContext()
        {
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal() }
            };
        }

        [Fact]
        public async Task AnalyzeScreenshot_ValidFile_ReturnsOkResult()
        {
            // Arrange
            SetUserContext("1");
            var mockFile = new Mock<IFormFile>();
            var expectedResult = new AnalysisResultDto { AnalysisId = 1 };

            _mockAiService.Setup(s => s.AnalyzeScreenshotAsync(mockFile.Object, 1, "VLTK Mobile"))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.AnalyzeScreenshot(mockFile.Object, SupportedGame.VLTK_Mobile);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<AnalysisResultDto>(okResult.Value);
            Assert.Equal(1, actualResult.AnalysisId);
        }

        [Fact]
        public async Task AnalyzeScreenshot_ServiceReturnsNull_ReturnsBadRequest()
        {
            // Arrange
            SetUserContext("1");
            var mockFile = new Mock<IFormFile>();

            _mockAiService.Setup(s => s.AnalyzeScreenshotAsync(mockFile.Object, 1, "VLTK Mobile"))
                .ReturnsAsync((AnalysisResultDto)null!);

            // Act
            var result = await _controller.AnalyzeScreenshot(mockFile.Object, SupportedGame.VLTK_Mobile);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeAutomatic_ValidCall_ReturnsOkResult()
        {
            // Arrange
            SetUserContext("2");
            var expectedResult = new AnalysisResultDto { AnalysisId = 1 };

            _mockAiService.Setup(s => s.AnalyzeLatestFromCloudAsync(2, "VLTK 2.0"))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.AnalyzeAutomatic(SupportedGame.VLTK_2_0);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<AnalysisResultDto>(okResult.Value);
            Assert.Equal(1, actualResult.AnalysisId);
        }

        [Fact]
        public async Task AnalyzeAutomatic_NoImageFound_ReturnsNotFound()
        {
            // Arrange
            SetUserContext("1");
            _mockAiService.Setup(s => s.AnalyzeLatestFromCloudAsync(1, "VLTK Mobile"))
                .ReturnsAsync((AnalysisResultDto)null!);

            // Act
            var result = await _controller.AnalyzeAutomatic(SupportedGame.VLTK_Mobile);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetList_ValidUser_ReturnsOkResult()
        {
            // Arrange
            SetUserContext("1", "user");
            var parameters = new AIQueryParameters();
            var expectedResult = new PagedResult<AnalysisResultDto>
            {
                Items = new List<AnalysisResultDto> { new AnalysisResultDto { AnalysisId = 1 } },
                TotalCount = 1
            };

            _mockAiService.Setup(s => s.GetHistoryAsync(1, "user", parameters))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetList(parameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<PagedResult<AnalysisResultDto>>(okResult.Value);
            Assert.Equal(1, actualResult.TotalCount);
        }

        [Fact]
        public async Task GetList_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            SetEmptyUserContext();
            var parameters = new AIQueryParameters();

            // Act
            var result = await _controller.GetList(parameters);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetById_ExistingId_ReturnsOkResult()
        {
            // Arrange
            int id = 1;
            var expectedResult = new AnalysisResultDto { AnalysisId = id };
            _mockAiService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<AnalysisResultDto>(okResult.Value);
            Assert.Equal(id, actualResult.AnalysisId);
        }

        [Fact]
        public async Task GetById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            int id = 999;
            _mockAiService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((AnalysisResultDto)null!);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetAnalysisResult_ExistingId_ReturnsOkResult()
        {
            // Arrange
            int id = 1;
            var expectedResult = new AnalysisResultDto { AnalysisId = id };
            _mockAiService.Setup(s => s.GetAnalysisResultAsync(id)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAnalysisResult(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAirtestUploads_ReturnsOkResult()
        {
            // Arrange
            var expectedUrls = new List<string> { "url1", "url2" };
            _mockAiService.Setup(s => s.GetAirtestUploadImagesAsync()).ReturnsAsync(expectedUrls);

            // Act
            var result = await _controller.GetAirtestUploads();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualUrls = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(2, actualUrls.Count);
        }

        [Fact]
        public async Task GetHeatmap_ReturnsOkResult()
        {
            // Arrange
            SetUserContext("1", "admin");
            var expectedData = new List<HeatmapDto> { new HeatmapDto() };
            _mockAiService.Setup(s => s.GetHeatmapDataAsync(1, "admin")).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetHeatmap();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task Delete_ExistingId_ReturnsOkResult()
        {
            // Arrange
            int id = 1;
            _mockAiService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockAiService.Verify(s => s.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task Delete_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            int id = 999;
            _mockAiService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
