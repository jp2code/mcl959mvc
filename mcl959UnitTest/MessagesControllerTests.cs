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
using Microsoft.EntityFrameworkCore;

public class MessagesControllerTests
{
    [Fact]
    public async Task Index_AdminUser_ReturnsViewWithMessages()
    {
        // Arrange: Use in-memory EF Core database
        var options = new DbContextOptionsBuilder<Mcl959DbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        using var dbContext = new Mcl959DbContext(options);

        // Seed test data
        dbContext.Messages.Add(new MessagesModel() { Id = 1, Name = "Test", Email = "test@example.com", Subject = "Test" });
        dbContext.SaveChanges();

        var userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        var cache = new Mock<IMemoryCache>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var smtpOptions = Options.Create(new SmtpSettings());
        var logger = new Mock<ILogger<Controller>>();

        var controller = new MessagesController(
            cache.Object, httpClientFactory.Object, dbContext, userManager.Object, smtpOptions, logger.Object)
        {
            IsAdmin = true
        };

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MessagesModel>>(viewResult.Model);
        Assert.Single(model); // Ensure the seeded message is present
    }
}