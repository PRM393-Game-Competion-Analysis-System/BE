using System;
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
    public class EventServiceTests
    {
        private readonly Mock<IEventRepository> _mockRepo;
        private readonly EventService _service;

        public EventServiceTests()
        {
            _mockRepo = new Mock<IEventRepository>();
            _service = new EventService(_mockRepo.Object);
        }

        private Event CreateSampleEvent(int id, string name, string type, string gameName)
        {
            return new Event
            {
                Eventid = id,
                Eventname = name,
                Eventtype = type,
                Startdate = DateTime.UtcNow,
                Enddate = DateTime.UtcNow.AddDays(7),
                Game = new Game { Gamename = gameName }
            };
        }

        [Fact]
        public void GetAll_ReturnsPagedResultOfEventDto()
        {
            // Arrange
            var parameters = new QueryParameters { PageNumber = 1, PageSize = 10 };
            var events = new List<Event>
            {
                CreateSampleEvent(1, "Event1", "Type1", "Game1"),
                CreateSampleEvent(2, "Event2", "Type2", "Game2")
            };
            int expectedTotalCount = 2;

            _mockRepo.Setup(r => r.GetAll(parameters, out expectedTotalCount)).Returns(events);

            // Act
            var result = _service.GetAll(parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("Event1", result.Items.First().EventName);
            Assert.Equal("Game1", result.Items.First().GameName);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsEventDto()
        {
            // Arrange
            int id = 1;
            var @event = CreateSampleEvent(id, "Event1", "Type1", "Game1");
            _mockRepo.Setup(r => r.GetById(id)).Returns(@event);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.EventId);
            Assert.Equal("Event1", result.EventName);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNull()
        {
            // Arrange
            int id = 99;
            _mockRepo.Setup(r => r.GetById(id)).Returns((Event)null!);

            // Act
            var result = _service.GetById(id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetByGame_ExistingGameId_ReturnsList()
        {
            // Arrange
            int gameId = 1;
            var events = new List<Event> { CreateSampleEvent(1, "Event1", "Type1", "Game1") };
            _mockRepo.Setup(r => r.GetByGame(gameId)).Returns(events);

            // Act
            var result = _service.GetByGame(gameId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Event1", result.First().EventName);
        }

        [Fact]
        public void SearchByName_Matches_ReturnsList()
        {
            // Arrange
            string name = "Event1";
            var events = new List<Event> { CreateSampleEvent(1, "Event1", "Type1", "Game1") };
            _mockRepo.Setup(r => r.SearchByName(name)).Returns(events);

            // Act
            var result = _service.SearchByName(name);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Event1", result.First().EventName);
        }

        [Fact]
        public void Add_CallsRepositoryAdd()
        {
            // Arrange
            var @event = new Event { Eventname = "NewEvent" };

            // Act
            _service.Add(@event);

            // Assert
            _mockRepo.Verify(r => r.Add(@event), Times.Once);
        }

        [Fact]
        public void Update_CallsRepositoryUpdate()
        {
            // Arrange
            var @event = new Event { Eventid = 1, Eventname = "UpdatedEvent" };

            // Act
            _service.Update(@event);

            // Assert
            _mockRepo.Verify(r => r.Update(@event), Times.Once);
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
