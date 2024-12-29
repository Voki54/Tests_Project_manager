using Microsoft.AspNetCore.Identity;
using Moq;
using Project_Manager.Models;

namespace Test_Project_Manager
{
    public static class FakeUserManager
    {
        public static Mock<UserManager<AppUser>> CreateMockUserManager()
        {
            var mockUserManager = new Mock<UserManager<AppUser>>(
                Mock.Of<IUserStore<AppUser>>(),
                null, null, null, null, null, null, null, null
            );

            // Настройка метода FindByIdAsync для возврата тестового пользователя
            mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new AppUser { Id = "user123", UserName = "testUser" });

            return mockUserManager;
        }
    }
}
