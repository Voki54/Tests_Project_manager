using Moq;
using Project_Manager.Data.DAO.Interfaces;
using Project_Manager.Events.Notification.EventHandlers;
using Project_Manager.Events.Notification;
using Project_Manager.Models.Enums;
using Project_Manager.Models;
using Project_Manager.Services.Interfaces;
using Project_Manager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Project_Manager.StatesManagers.Interfaces;

namespace Test_Project_Manager
{
    [TestFixture]
    public class JoinProjectServiceTests
    {
        private Mock<IProjectService> _mockProjectService;
        private Mock<IProjectUserService> _mockProjectUserService;
        private Mock<IJoinProjectRequestRepository> _mockJoinProjectRequestRepository;
        private Mock<UserManager<AppUser>> _mockUserManager;
        private Mock<EventPublisher> _mockEventPublisher;
        private Mock<NotificationEventHandler> _mockNotificationEventHandler;
        private JoinProjectService _service;


        [SetUp]
        public void Setup()
        {
            _mockProjectService = new Mock<IProjectService>();
            _mockProjectUserService = new Mock<IProjectUserService>();
            _mockJoinProjectRequestRepository = new Mock<IJoinProjectRequestRepository>();
            _mockUserManager = FakeUserManager.CreateMockUserManager();
            _mockEventPublisher = new Mock<EventPublisher>();

            var mockNotificationStateManager = new Mock<INotificationStatesManager>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockLogger = new Mock<ILogger<NotificationService>>();

            _mockNotificationEventHandler = new Mock<NotificationEventHandler>(
                mockNotificationStateManager.Object,
                mockNotificationService.Object,
                _mockProjectUserService.Object,
                _mockUserManager.Object,
                _mockProjectService.Object,
                mockLogger.Object
            );

            _service = new JoinProjectService(
                _mockProjectService.Object,
                _mockProjectUserService.Object,
                _mockUserManager.Object,
                _mockJoinProjectRequestRepository.Object,
                _mockEventPublisher.Object,
                _mockNotificationEventHandler.Object);
        }

        [Test]
        public async Task SubmitJoinRequestAsync_ShouldReturnFalse_WhenProjectDoesNotExist()
        {
            // Arrange
            _mockProjectService.Setup(ps => ps.ExistProjectAsync(It.IsAny<int>())).ReturnsAsync(false);

            // Act
            var result = await _service.SubmitJoinRequestAsync(1, "user123");

            // Assert
            Assert.IsFalse(result);
            _mockJoinProjectRequestRepository.Verify(repo => repo.CreateAsync(It.IsAny<JoinProjectRequest>()), Times.Never);
        }

        [Test]
        public async Task SubmitJoinRequestAsync_ShouldReturnTrue_WhenUserAlreadySubmittedRequest()
        {
            // Arrange
            _mockProjectService.Setup(ps => ps.ExistProjectAsync(It.IsAny<int>())).ReturnsAsync(true);
            _mockJoinProjectRequestRepository.Setup(repo => repo.GetJoinProjectRequestAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new JoinProjectRequest(1, "user123", JoinProjectRequestStatus.Pending));

            // Act
            var result = await _service.SubmitJoinRequestAsync(1, "user123");

            // Assert
            Assert.IsTrue(result);
            _mockJoinProjectRequestRepository.Verify(repo => repo.CreateAsync(It.IsAny<JoinProjectRequest>()), Times.Never);
        }

        [Test]
        public async Task SubmitJoinRequestAsync_ShouldCreateRequestAndPublishEvent_WhenUserNotInProject()
        {
            // Arrange
            _mockProjectService.Setup(ps => ps.ExistProjectAsync(It.IsAny<int>())).ReturnsAsync(true);
            _mockJoinProjectRequestRepository.Setup(repo => repo.GetJoinProjectRequestAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((JoinProjectRequest?)null);
            _mockProjectUserService.Setup(pus => pus.IsUserInProjectAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.SubmitJoinRequestAsync(1, "user123");

            // Assert
            Assert.IsTrue(result);
            _mockJoinProjectRequestRepository.Verify(repo => repo.CreateAsync(It.Is<JoinProjectRequest>(
                r => r.ProjectId == 1 && r.UserId == "user123" && r.Status == JoinProjectRequestStatus.Pending
            )), Times.Once);
        }

        [Test]
        public async Task SubmitJoinRequestAsync_ShouldAcceptRequest_WhenUserAlreadyInProject()
        {
            // Arrange
            _mockProjectService.Setup(ps => ps.ExistProjectAsync(It.IsAny<int>())).ReturnsAsync(true);
            _mockProjectUserService.Setup(pus => pus.IsUserInProjectAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            _mockJoinProjectRequestRepository.Setup(repo => repo.GetJoinProjectRequestAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((JoinProjectRequest?)null);

            // Act
            var result = await _service.SubmitJoinRequestAsync(1, "user123");

            // Assert
            Assert.IsTrue(result);
            _mockJoinProjectRequestRepository.Verify(repo => repo.CreateAsync(It.Is<JoinProjectRequest>(
                r => r.ProjectId == 1 && r.UserId == "user123" && r.Status == JoinProjectRequestStatus.Accepted
            )), Times.Once);           
        }
    }
}
