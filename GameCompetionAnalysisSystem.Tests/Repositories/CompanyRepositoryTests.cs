using System;
using System.Linq;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Repositories
{
    public class CompanyRepositoryTests
    {
        private PRM393GameAiContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PRM393GameAiContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new PRM393GameAiContext(options);
        }

        [Fact]
        public void Add_ValidCompany_AddsToDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new CompanyRepository(context);
            var company = new Company { Companyname = "VNG", Country = "VN" };

            // Act
            repo.Add(company);

            // Assert
            var result = context.Companies.FirstOrDefault(c => c.Companyname == "VNG");
            Assert.NotNull(result);
            Assert.Equal("VN", result.Country);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsCompany()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new CompanyRepository(context);
            var company = new Company { Companyname = "VNG", Country = "VN" };
            repo.Add(company);

            // Act
            var result = repo.GetById(company.Companyid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("VNG", result.Companyname);
        }

        [Fact]
        public void GetAll_WithSearchAndSort_ReturnsCorrectDataAndCount()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new CompanyRepository(context);
            repo.Add(new Company { Companyname = "VNG", Country = "VN" });
            repo.Add(new Company { Companyname = "Tencent", Country = "CN" });
            repo.Add(new Company { Companyname = "Soha", Country = "VN" });

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "vng",
                SortBy = "companyname",
                IsDescending = false
            };

            // Act
            var result = repo.GetAll(parameters, out int totalCount);

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Single(result);
            Assert.Equal("VNG", result[0].Companyname);
        }

        [Fact]
        public void GetAll_WithFilter_ReturnsCorrectDataAndCount()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new CompanyRepository(context);
            repo.Add(new Company { Companyname = "VNG", Country = "VN" });
            repo.Add(new Company { Companyname = "Tencent", Country = "CN" });
            repo.Add(new Company { Companyname = "Soha", Country = "VN" });

            var parameters = new QueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                Filter = "cn"
            };

            // Act
            var result = repo.GetAll(parameters, out int totalCount);

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Single(result);
            Assert.Equal("Tencent", result[0].Companyname);
        }

        [Fact]
        public void Update_ExistingCompany_UpdatesDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new CompanyRepository(context);
            var company = new Company { Companyname = "VNG", Country = "VN" };
            repo.Add(company);

            // Act
            company.Country = "Global";
            repo.Update(company);

            // Assert
            var result = context.Companies.Find(company.Companyid);
            Assert.NotNull(result);
            Assert.Equal("Global", result.Country);
        }

        [Fact]
        public void Delete_ExistingCompany_RemovesFromDatabase()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new CompanyRepository(context);
            var company = new Company { Companyname = "VNG", Country = "VN" };
            repo.Add(company);

            // Act
            repo.Delete(company.Companyid);

            // Assert
            var result = context.Companies.Find(company.Companyid);
            Assert.Null(result);
        }
    }
}
