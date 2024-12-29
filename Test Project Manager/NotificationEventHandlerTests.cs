using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Project_Manager.Events.Notification.EventHandlers;
using Project_Manager.Events.Notification;
using Project_Manager.Models;
using Project_Manager.Services.Interfaces;
using Project_Manager.Services;
using Project_Manager.StatesManagers.Interfaces;
using Project_Manager.Models.Enums;

namespace Test_Project_Manager;


[TestFixture]
public class NotificationEventHandlerTests
{
    private Mock<INotificationStatesManager> _mockNotificationStateManager;
    private Mock<INotificationService> _mockNotificationService;
    private Mock<IProjectUserService> _mockProjectUserService;
    private Mock<IProjectService> _mockProjectService;
    private Mock<UserManager<AppUser>> _mockUserManager;
    private Mock<ILogger<NotificationService>> _mockLogger;
    private NotificationEventHandler _notificationEventHandler;

    [SetUp]
    public void Setup()
    {
        // Инициализация моков
        _mockNotificationStateManager = new Mock<INotificationStatesManager>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockProjectUserService = new Mock<IProjectUserService>();
        _mockProjectService = new Mock<IProjectService>();
        _mockLogger = new Mock<ILogger<NotificationService>>();

        // Создание мока UserManager с помощью CreateMockUserManager
        _mockUserManager = FakeUserManager.CreateMockUserManager();

        // Инициализация NotificationEventHandler с моками
        _notificationEventHandler = new NotificationEventHandler(
            _mockNotificationStateManager.Object,
            _mockNotificationService.Object,
            _mockProjectUserService.Object,
            _mockUserManager.Object,
            _mockProjectService.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task HandleAsync_ShouldCreateNotification_WhenValidJoinProjectEvent()
    {
        // Arrange
        var notificationEvent = NotificationSendingEvent.CreateWithSender("user123", 1, NotificationType.JoinProject);

        _mockProjectService.Setup(ps => ps.ExistProjectAsync(notificationEvent.ProjectId)).ReturnsAsync(true);
        _mockProjectUserService.Setup(pus => pus.GetAdminIdAsync(notificationEvent.ProjectId)).ReturnsAsync("admin123");
        _mockProjectService.Setup(ps => ps.GetProjectName(notificationEvent.ProjectId)).ReturnsAsync("Test Project");
        _mockNotificationService.Setup(x => x.CreateAsync(It.IsAny<Notification>())).ReturnsAsync(true);
        _mockNotificationStateManager.Setup(x => x.ChangeNotificationState(It.IsAny<Notification>(), It.IsAny<NotificationState?>())).ReturnsAsync(true);

        // Act
        await _notificationEventHandler.HandleAsync(notificationEvent);

        // Assert
        _mockNotificationStateManager.Verify(x => x.ChangeNotificationState(It.IsAny<Notification>(), It.IsAny<NotificationState?>()), Times.Once);
        _mockNotificationService.Verify(ns => ns.CreateAsync(It.IsAny<Notification>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ShouldCreateNotification_WhenValidAcceptJoinEvent()
    {
        // Arrange
        var notificationEvent = NotificationSendingEvent.CreateWithRecipient("user123", 1, NotificationType.AcceptJoin);

        _mockProjectService.Setup(ps => ps.ExistProjectAsync(notificationEvent.ProjectId)).ReturnsAsync(true);
        _mockUserManager.Setup(um => um.FindByIdAsync(notificationEvent.RecipientId)).ReturnsAsync(new AppUser { Id = "user123", UserName = "testUser" });
        _mockProjectService.Setup(ps => ps.GetProjectName(notificationEvent.ProjectId)).ReturnsAsync("Test Project");
        _mockNotificationService.Setup(x => x.CreateAsync(It.IsAny<Notification>())).ReturnsAsync(true);
        _mockNotificationStateManager.Setup(x => x.ChangeNotificationState(It.IsAny<Notification>(), It.IsAny<NotificationState?>())).ReturnsAsync(true);

        // Act
        await _notificationEventHandler.HandleAsync(notificationEvent);

        // Assert
        _mockNotificationStateManager.Verify(x => x.ChangeNotificationState(It.IsAny<Notification>(), It.IsAny<NotificationState?>()), Times.Once);
        _mockNotificationService.Verify(ns => ns.CreateAsync(It.IsAny<Notification>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ShouldThrowInvalidOperationException_WhenUnknownNotificationType()
    {
        // Arrange
        var notificationEvent = NotificationSendingEvent.CreateWithRecipient("user123", 1, (NotificationType)999);
        _mockProjectService.Setup(ps => ps.ExistProjectAsync(notificationEvent.ProjectId)).ReturnsAsync(true);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => _notificationEventHandler.HandleAsync(notificationEvent));
        Assert.AreEqual("Unknown notification event type.", exception.Message);
    }
}
