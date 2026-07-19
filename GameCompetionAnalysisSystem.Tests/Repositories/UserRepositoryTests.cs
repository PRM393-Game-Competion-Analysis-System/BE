using System;
using System.Linq;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private PRM393GameAiContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PRM393GameAiContext(options);
        }

        [Fact]
        public void Add_ValidUser_AddsToDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new UserRepository(context);
            var user = new User { Username = "TestUser", Email = "test@example.com", Role = "user" };

            // Act
            repo.Add(user);

            // Assert
            var result = context.Users.FirstOrDefault(u => u.Username == "TestUser");
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsUser()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new UserRepository(context);
            var user = new User { Username = "TestUser", Email = "test@example.com" };
            repo.Add(user);

            // Act
            var result = repo.GetById(user.Userid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestUser", result.Username);
        }

        [Fact]
        public void GetAll_WithSearchAndPagination_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new UserRepository(context);
            repo.Add(new User { Username = "Admin", Email = "admin@example.com", Role = "admin" });
            repo.Add(new User { Username = "User1", Email = "user1@example.com", Role = "user" });
            repo.Add(new User { Username = "User2", Email = "user2@example.com", Role = "user" });

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "user"
            };

            // Act
            var result = repo.GetAll(parameters, out int totalCount);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.Username == "User1");
        }

        [Fact]
        public void GetAll_WithFilter_ReturnsCorrectData()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new UserRepository(context);
            repo.Add(new User { Username = "Admin", Email = "admin@example.com", Role = "admin" });
            repo.Add(new User { Username = "User1", Email = "user1@example.com", Role = "user" });
            repo.Add(new User { Username = "User2", Email = "user2@example.com", Role = "user" });

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                Filter = "admin"
            };

            // Act
            var result = repo.GetAll(parameters, out int totalCount);

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Single(result);
            Assert.Equal("Admin", result[0].Username);
        }

        [Fact]
        public void Update_ExistingUser_UpdatesDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new UserRepository(context);
            var user = new User { Username = "TestUser", Email = "test@example.com" };
            repo.Add(user);

            // Act
            user.Email = "new@example.com";
            repo.Update(user);

            // Assert
            var result = context.Users.Find(user.Userid);
            Assert.NotNull(result);
            Assert.Equal("new@example.com", result.Email);
        }

        [Fact]
        public void Delete_ExistingUser_RemovesFromDatabaseWithRelatedEntities()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new UserRepository(context);
            var user = new User 
            { 
                Username = "TestUser",
                Imageuploads = new System.Collections.Generic.List<Imageupload>
                {
                    new Imageupload 
                    {
                        Imageurl = "url",
                        Aianalyses = new System.Collections.Generic.List<Aianalysis>
                        {
                            new Aianalysis { Confidencescore = 0.9 }
                        }
                    }
                }
            };
            repo.Add(user);

            // Act
            repo.Delete(user.Userid);

            // Assert
            var result = context.Users.Find(user.Userid);
            Assert.Null(result);
            Assert.Empty(context.Imageuploads); // Related entities should also be deleted
            Assert.Empty(context.Aianalyses);
        }
    }
}
