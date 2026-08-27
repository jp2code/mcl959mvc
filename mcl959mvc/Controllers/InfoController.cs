using mcl959mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Controllers
{
    public class InfoController : Controller
    {
        private readonly Mcl959DbContext _db;

        public InfoController(Mcl959DbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ToysForTots() => View();

        public IActionResult SpaghettiFeed() => View();

        public IActionResult GunRaffle() => View();

        public async Task<IActionResult> Maintenance() {
            var commandant = await _db.MemberRanks
                .Where(r => r.DisplayRank == "Commandant")
                .Join(_db.Roster,
                    rank => rank.MemberNumber,
                    member => member.MemberNumber,
                    (rank, member) => member)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            ViewBag.Commandant = commandant; // null if none found
            var buildingSuper = await _db.MemberRanks
                .Where(r => r.DisplayRank == "Building Superintendent")
                .Join(_db.Roster,
                    rank => rank.MemberNumber,
                    member => member.MemberNumber,
                    (rank, member) => member)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            ViewBag.BuildingSuuper = buildingSuper;
            return View();
        }
    }
}
