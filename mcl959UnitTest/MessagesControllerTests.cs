using Xunit;
using Moq;
using mcl959mvc.Controllers;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using mcl959mvc.Classes;
using Microsoft.Extensions.Logging;

public class MessagesControllerTests
{
    [Fact]
    public async Task Index_AdminUser_ReturnsViewWithMessages()
    {
        // Arrange
        var dbContext = new Mock<Mcl959DbContext>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), "", "", "", "", "", "", "", "");
        var cache = new Mock<IMemoryCache>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var smtpOptions = Options.Create(new SmtpSettings());
        var logger = new Mock<ILogger<Controller>>();

        var controller = new MessagesController(
            cache.Object, httpClientFactory.Object, dbContext.Object, userManager.Object, smtpOptions, logger.Object)
        {
            IsAdmin = true
        };

        // Act
        var result = await controller.Index();

        // Assert
        Assert.IsType<ViewResult>(result);
    }
}