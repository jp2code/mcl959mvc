using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Controllers;

public class RosterController : Controller
{
    private readonly Mcl959DbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public RosterController(Mcl959DbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Roster
    public async Task<IActionResult> Index()
    {
        return View(await _context.Roster.ToListAsync());
    }

    // GET: Roster/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var member = await _context.Roster.FindAsync(id);
        if (member == null) return NotFound();
        return View(member);
    }

    // GET: Roster/Create
    public async Task<IActionResult> Create()
    {
        if (!await IsCurrentUserAdmin()) return Forbid();
        return View();
    }

    // POST: Roster/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Roster member)
    {
        if (!await IsCurrentUserAdmin()) return Forbid();
        if (ModelState.IsValid)
        {
            _context.Add(member);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(member);
    }

    // GET: Roster/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (!await IsCurrentUserAdmin()) return Forbid();
        if (id == null) return NotFound();
        var member = await _context.Roster.FindAsync(id);
        if (member == null) return NotFound();
        return View(member);
    }

    // POST: Roster/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Roster member)
    {
        if (!await IsCurrentUserAdmin()) return Forbid();
        if (id != member.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _context.Update(member);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(member);
    }

    // GET: Roster/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (!await IsCurrentUserAdmin()) return Forbid();
        if (id == null) return NotFound();
        var member = await _context.Roster.FindAsync(id);
        if (member == null) return NotFound();
        return View(member);
    }

    // POST: Roster/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await IsCurrentUserAdmin()) return Forbid();
        var member = await _context.Roster.FindAsync(id);
        if (member != null)
        {
            _context.Roster.Remove(member);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsCurrentUserAdmin()
    {
        if (User.Identity?.IsAuthenticated != true) return false;
        var user = await _userManager.GetUserAsync(User);
        return user?.IsAdmin == true;
    }
}