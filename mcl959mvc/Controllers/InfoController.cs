using Microsoft.AspNetCore.Mvc;

namespace mcl959mvc.Controllers
{
    public class InfoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ToysForTots() => View();

        public IActionResult SpaghettiFeed() => View();

        public IActionResult GunRaffle() => View();

        public IActionResult Maintenance() => View();
    }
}
