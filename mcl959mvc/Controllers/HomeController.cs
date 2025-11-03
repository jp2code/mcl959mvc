using mcl959mvc.Classes;
using mcl959mvc.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace mcl959mvc.Controllers
{
    public class HomeController : Mcl959MemberController
    {
        public HomeController(
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            ILogger<Controller> logger,
            IOptions<SmtpSettings> smtpSettings)
            : base(userManager, logger, smtpSettings)
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
           return View();
        }

        public IActionResult ChatPanel()
        {
            return PartialView("_ChatPanel");
        }
    }
}
