using System.Collections.Generic;
using BIL.Service;
using DAL.DTO;
using DAL.Entities;
using GameCompetionAnalysisSystem.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameCompetionAnalysisSystem.Tests.Controllers
{
    public class CompaniesControllerTests
    {
        private readonly Mock<ICompanyService> _mockCompanyService;
        private readonly CompaniesController _controller;

        public CompaniesControllerTests()
        {
            _mockCompanyService = new Mock<ICompanyService>();
            _controller = new CompaniesController(_mockCompanyService.Object);
        }

        [Fact]
        public void GetList_ReturnsOkResult_WithCompanies()
        {
            // Arrange
            var parameters = new QueryParameters();
            var expectedResult = new PagedResult<CompanyDto>
            {
                Items = new List<CompanyDto> { new CompanyDto { CompanyId = 1, CompanyName = "TestCompany" } },
                TotalCount = 1
            };

            _mockCompanyService.Setup(s => s.GetAll(parameters)).Returns(expectedResult);

            // Act
            var result = _controller.GetList(parameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<PagedResult<CompanyDto>>(okResult.Value);
            Assert.Equal(1, actualResult.TotalCount);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsOkResult_WithCompany()
        {
            // Arrange
            int id = 1;
            var expectedCompany = new CompanyDto { CompanyId = id, CompanyName = "TestCompany" };

            _mockCompanyService.Setup(s => s.GetById(id)).Returns(expectedCompany);

            // Act
            var result = _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualCompany = Assert.IsType<CompanyDto>(okResult.Value);
            Assert.Equal(id, actualCompany.CompanyId);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            int id = 999;
            _mockCompanyService.Setup(s => s.GetById(id)).Returns((CompanyDto)null!);

            // Act
            var result = _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Create_ValidCompany_ReturnsOkResult_WithCreatedCompanyDto()
        {
            // Arrange
            var companyToCreate = new Company { Companyid = 1, Companyname = "NewCompany", Country = "US", Website = "http://test.com" };

            // Act
            var result = _controller.Create(companyToCreate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdCompanyDto = Assert.IsType<CompanyDto>(okResult.Value);
            Assert.Equal(companyToCreate.Companyid, createdCompanyDto.CompanyId);
            Assert.Equal(companyToCreate.Companyname, createdCompanyDto.CompanyName);
            
            _mockCompanyService.Verify(s => s.Add(companyToCreate), Times.Once);
        }

        [Fact]
        public void Update_ValidCompany_ReturnsOkResult_WithUpdatedCompanyDto()
        {
            // Arrange
            int id = 1;
            var companyToUpdate = new Company { Companyname = "UpdatedCompany", Country = "UK", Website = "http://updated.com" };

            // Act
            var result = _controller.Update(id, companyToUpdate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedCompanyDto = Assert.IsType<CompanyDto>(okResult.Value);
            
            Assert.Equal(id, companyToUpdate.Companyid);
            Assert.Equal(id, updatedCompanyDto.CompanyId);
            Assert.Equal(companyToUpdate.Companyname, updatedCompanyDto.CompanyName);

            _mockCompanyService.Verify(s => s.Update(companyToUpdate), Times.Once);
        }

        [Fact]
        public void Delete_ReturnsOkResult()
        {
            // Arrange
            int id = 1;

            // Act
            var result = _controller.Delete(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockCompanyService.Verify(s => s.Delete(id), Times.Once);
        }
    }
}
