using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using GameCompetionAnalysisSystem.Services;
using GameCompetionAnalysisSystem.Models;

namespace GameCompetionAnalysisSystem.Tests
{
    public class OcrServiceTests
    {
        [Fact]
        public async Task ExtractTextAsync_ReturnsOcrResult_OnSuccess()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OcrApi:BaseUrl"]).Returns("http://fakeurl");

            var expectedOcrResult = new OcrResult 
            { 
                Success = true, 
                FullText = "Test text" 
            };
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(expectedOcrResult))
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var service = new OcrService(httpClient, mockConfig.Object);

            var fileMock = new Mock<IFormFile>();
            var content = "Fake image content";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns("test.png");
            fileMock.Setup(f => f.ContentType).Returns("image/png");

            // Act
            var result = await service.ExtractTextAsync(fileMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Test text", result.FullText);
        }

        [Fact]
        public async Task ExtractTextAsync_ThrowsHttpRequestException_OnFailure()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OcrApi:BaseUrl"]).Returns("http://fakeurl");

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var service = new OcrService(httpClient, mockConfig.Object);

            var fileMock = new Mock<IFormFile>();
            var ms = new MemoryStream();
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns("test.png");
            fileMock.Setup(f => f.ContentType).Returns("image/png");

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => service.ExtractTextAsync(fileMock.Object));
        }
    }
}
