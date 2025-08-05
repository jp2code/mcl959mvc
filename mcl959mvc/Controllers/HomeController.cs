using Microsoft.AspNetCore.Mvc;

namespace mcl959mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            string logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");
            Console.WriteLine(logoPath);
            return View();
        }
    }
}
