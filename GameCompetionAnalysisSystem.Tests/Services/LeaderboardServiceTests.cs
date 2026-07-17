using System.Linq;
using System.Threading.Tasks;
using Xunit;
using GameCompetionAnalysisSystem.Services;

namespace GameCompetionAnalysisSystem.Tests
{
    public class LeaderboardServiceTests
    {
        [Fact]
        public async Task ProcessOcrAsync_DoesNotThrow()
        {
            // Arrange
            var service = new LeaderboardService();

            // Act
            var exception = await Record.ExceptionAsync(() => service.ProcessOcrAsync(1));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task GetTopAsync_WithEmptyEntries_ReturnsEmpty()
        {
            // Arrange
            var service = new LeaderboardService();

            // Act
            var result = await service.GetTopAsync(10);

            // Assert
            Assert.Empty(result);
        }
    }
}
