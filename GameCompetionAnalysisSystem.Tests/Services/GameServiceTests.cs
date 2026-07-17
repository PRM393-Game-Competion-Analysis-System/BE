using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using GameCompetionAnalysisSystem.Models;
using GameCompetionAnalysisSystem.Services;

namespace GameCompetionAnalysisSystem.Tests
{
    public class GameServiceTests
    {
        [Fact]
        public void GetAllGames_EmptyStore_ReturnsEmpty()
        {
            // Arrange
            var service = new GameService();

            // Act
            var result = service.GetAllGames();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAllGames_WithElements_ReturnsAll()
        {
            // Arrange
            var service = new GameService();
            service.Add(new Game { Name = "Game1" });
            service.Add(new Game { Name = "Game2" });

            // Act
            var result = service.GetAllGames();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetById_ExistingId_ReturnsGame()
        {
            // Arrange
            var service = new GameService();
            service.Add(new Game { Name = "Game1" });
            var expectedGame = service.GetAllGames().First();

            // Act
            var result = service.GetById(expectedGame.Gameid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedGame.Gameid, result.Gameid);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNull()
        {
            // Arrange
            var service = new GameService();

            // Act
            var result = service.GetById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Add_EmptyStore_AssignsId1()
        {
            // Arrange
            var service = new GameService();
            var game = new Game { Name = "Game1" };

            // Act
            service.Add(game);

            // Assert
            Assert.Equal(1, game.Gameid);
            Assert.Single(service.GetAllGames());
        }

        [Fact]
        public void Add_NonEmptyStore_AssignsMaxIdPlusOne()
        {
            // Arrange
            var service = new GameService();
            service.Add(new Game { Name = "Game1" }); // Gets ID 1
            var game2 = new Game { Name = "Game2" };

            // Act
            service.Add(game2);

            // Assert
            Assert.Equal(2, game2.Gameid);
            Assert.Equal(2, service.GetAllGames().Count());
        }

        [Fact]
        public void Update_ExistingGame_UpdatesProperties()
        {
            // Arrange
            var service = new GameService();
            service.Add(new Game { Name = "OldName", Genre = "OldGenre", Publisher = "OldPublisher" });
            var existingGameId = service.GetAllGames().First().Gameid;

            var updatedGame = new Game
            {
                Gameid = existingGameId,
                Name = "NewName",
                Genre = "NewGenre",
                Publisher = "NewPublisher"
            };

            // Act
            service.Update(updatedGame);

            // Assert
            var result = service.GetById(existingGameId);
            Assert.NotNull(result);
            Assert.Equal("NewName", result.Name);
            Assert.Equal("NewGenre", result.Genre);
            Assert.Equal("NewPublisher", result.Publisher);
        }

        [Fact]
        public void Update_NonExistingGame_DoesNothing()
        {
            // Arrange
            var service = new GameService();
            service.Add(new Game { Name = "Game1" });
            var nonExistingGame = new Game { Gameid = 999, Name = "NonExisting" };

            // Act
            service.Update(nonExistingGame);

            // Assert
            var allGames = service.GetAllGames();
            Assert.Single(allGames);
            Assert.Equal("Game1", allGames.First().Name);
        }

        [Fact]
        public void Delete_ExistingGame_RemovesFromStore()
        {
            // Arrange
            var service = new GameService();
            service.Add(new Game { Name = "Game1" });
            var existingGameId = service.GetAllGames().First().Gameid;

            // Act
            service.Delete(existingGameId);

            // Assert
            Assert.Empty(service.GetAllGames());
        }

        [Fact]
        public void Delete_NonExistingGame_DoesNothing()
        {
            // Arrange
            var service = new GameService();
            service.Add(new Game { Name = "Game1" });

            // Act
            service.Delete(999);

            // Assert
            Assert.Single(service.GetAllGames());
        }

        [Fact]
        public void GetMMORPGGames_WithMatchingGenreCaseInsensitive_ReturnsGames()
        {
            // Arrange
            var service = new GameService();
            service.Add(new Game { Genre = "MMORPG" });
            service.Add(new Game { Genre = "mmorpg" });
            service.Add(new Game { Genre = "Action" });

            // Act
            var result = service.GetMMORPGGames();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetMMORPGGames_WithNullGenre_DoesNotThrowAndDoesNotReturn()
        {
            // Arrange
            var service = new GameService();
            service.Add(new Game { Genre = null });

            // Act
            var result = service.GetMMORPGGames();

            // Assert
            Assert.Empty(result);
        }
    }
}
