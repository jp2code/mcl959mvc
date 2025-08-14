using mcl959mvc.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace mcl959mvc.Controllers
{
    public class HomeController : Mcl959MemberController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, ILogger<Controller> logger)
            : base(userManager, logger)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
