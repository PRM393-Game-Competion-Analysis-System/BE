using System.Collections.Generic;
using System.Linq;
using BIL.Service;
using DAL.DTO;
using DAL.Entities;
using DAL.Repository;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Services
{
    public class CompanyServiceTests
    {
        private readonly Mock<ICompanyRepository> _mockRepo;
        private readonly CompanyService _service;

        public CompanyServiceTests()
        {
            _mockRepo = new Mock<ICompanyRepository>();
            _service = new CompanyService(_mockRepo.Object);
        }

        [Fact]
        public void GetAll_ReturnsPagedResultOfCompanyDto()
        {
            // Arrange
            var parameters = new QueryParameters { PageNumber = 1, PageSize = 10 };
            var companies = new List<Company>
            {
                new Company { Companyid = 1, Companyname = "Company1", Country = "USA", Website = "url1" },
                new Company { Companyid = 2, Companyname = "Company2", Country = "UK", Website = "url2" }
            };
            int expectedTotalCount = 2;

            _mockRepo.Setup(r => r.GetAll(parameters, out expectedTotalCount)).Returns(companies);

            // Act
            var result = _service.GetAll(parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("Company1", result.Items.First().CompanyName);
            Assert.Equal("USA", result.Items.First().Country);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsCompanyDto()
        {
            // Arrange
            int id = 1;
            var company = new Company { Companyid = id, Companyname = "Company1", Country = "USA", Website = "url1" };
            _mockRepo.Setup(r => r.GetById(id)).Returns(company);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.CompanyId);
            Assert.Equal("Company1", result.CompanyName);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNull()
        {
            // Arrange
            int id = 99;
            _mockRepo.Setup(r => r.GetById(id)).Returns((Company)null!);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Add_CallsRepositoryAdd()
        {
            // Arrange
            var company = new Company { Companyname = "NewCompany" };

            // Act
            _service.Add(company);

            // Assert
            _mockRepo.Verify(r => r.Add(company), Times.Once);
        }

        [Fact]
        public void Update_CallsRepositoryUpdate()
        {
            // Arrange
            var company = new Company { Companyid = 1, Companyname = "UpdatedCompany" };

            // Act
            _service.Update(company);

            // Assert
            _mockRepo.Verify(r => r.Update(company), Times.Once);
        }

        [Fact]
        public void Delete_CallsRepositoryDelete()
        {
            // Arrange
            int id = 1;

            // Act
            _service.Delete(id);

            // Assert
            _mockRepo.Verify(r => r.Delete(id), Times.Once);
        }
    }
}
