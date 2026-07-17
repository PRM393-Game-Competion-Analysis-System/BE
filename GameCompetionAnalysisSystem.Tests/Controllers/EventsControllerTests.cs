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
    public class EventsControllerTests
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly EventsController _controller;

        public EventsControllerTests()
        {
            _mockEventService = new Mock<IEventService>();
            _controller = new EventsController(_mockEventService.Object);
        }

        [Fact]
        public void GetList_ReturnsOkResult_WithEvents()
        {
            // Arrange
            var parameters = new QueryParameters();
            var expectedResult = new PagedResult<EventDto>
            {
                Items = new List<EventDto> { new EventDto { EventId = 1, EventName = "TestEvent" } },
                TotalCount = 1
            };

            _mockEventService.Setup(s => s.GetAll(parameters)).Returns(expectedResult);

            // Act
            var result = _controller.GetList(parameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<PagedResult<EventDto>>(okResult.Value);
            Assert.Equal(1, actualResult.TotalCount);
        }

        [Fact]
        public void Search_ReturnsOkResult_WithMatchingEvents()
        {
            // Arrange
            var searchName = "Test";
            var expectedResult = new List<EventDto> { new EventDto { EventId = 1, EventName = "TestEvent" } };

            _mockEventService.Setup(s => s.SearchByName(searchName)).Returns(expectedResult);

            // Act
            var result = _controller.Search(searchName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<EventDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsOkResult_WithEvent()
        {
            // Arrange
            int id = 1;
            var expectedEvent = new EventDto { EventId = id, EventName = "TestEvent" };

            _mockEventService.Setup(s => s.GetById(id)).Returns(expectedEvent);

            // Act
            var result = _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualEvent = Assert.IsType<EventDto>(okResult.Value);
            Assert.Equal(id, actualEvent.EventId);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            int id = 999;
            _mockEventService.Setup(s => s.GetById(id)).Returns((EventDto)null!);

            // Act
            var result = _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void GetByGame_ReturnsOkResult_WithEvents()
        {
            // Arrange
            int gameId = 1;
            var expectedResult = new List<EventDto> { new EventDto { EventId = 1, EventName = "TestEvent" } };

            _mockEventService.Setup(s => s.GetByGame(gameId)).Returns(expectedResult);

            // Act
            var result = _controller.GetByGame(gameId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<List<EventDto>>(okResult.Value);
            Assert.Single(actualResult);
        }

        [Fact]
        public void Create_ValidEvent_ReturnsOkResult_WithCreatedEventDto()
        {
            // Arrange
            var eventToCreate = new Event { Eventid = 1, Eventname = "NewEvent", Eventtype = "Tournament" };

            // Act
            var result = _controller.Create(eventToCreate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdEventDto = Assert.IsType<EventDto>(okResult.Value);
            Assert.Equal(eventToCreate.Eventid, createdEventDto.EventId);
            Assert.Equal(eventToCreate.Eventname, createdEventDto.EventName);
            
            _mockEventService.Verify(s => s.Add(eventToCreate), Times.Once);
        }

        [Fact]
        public void Update_ValidEvent_ReturnsOkResult_WithUpdatedEventDto()
        {
            // Arrange
            int id = 1;
            var eventToUpdate = new Event { Eventname = "UpdatedEvent", Eventtype = "Competition" };

            // Act
            var result = _controller.Update(id, eventToUpdate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedEventDto = Assert.IsType<EventDto>(okResult.Value);
            
            Assert.Equal(id, eventToUpdate.Eventid);
            Assert.Equal(id, updatedEventDto.EventId);
            Assert.Equal(eventToUpdate.Eventname, updatedEventDto.EventName);

            _mockEventService.Verify(s => s.Update(eventToUpdate), Times.Once);
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
            _mockEventService.Verify(s => s.Delete(id), Times.Once);
        }
    }
}
