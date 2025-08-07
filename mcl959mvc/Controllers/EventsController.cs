using mcl959mvc;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Controllers;

public class EventsController : Controller
{
    private readonly Mcl959DbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    // these are used to determine if the user is an admin or registered user
    private bool _isAdmin = false;
    private bool _isRegistered = false;

    public EventsController(Mcl959DbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
        _isRegistered = User.HasClaim("isRegistered", "true");
        _isAdmin = User.HasClaim("isAdmin", "true");
    }

    // GET: Events
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            _isRegistered = user?.IsRegistered ?? false;
            if (_isRegistered)
            {
                _isAdmin = user?.IsAdmin ?? false;
            }
        }
        ViewBag.IsRegistered = _isRegistered;
        ViewBag.IsAdmin = _isAdmin;
        return View(await _context.Events.ToListAsync());
    }

    // GET: Events/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var evt = await _context.Events.FindAsync(id);
        if (evt == null) return NotFound();
        return View(evt);
    }

    // GET: Events/Create
    public async Task<IActionResult> Create()
    {
        if (!_isAdmin) return Forbid();
        return View();
    }

    // POST: Events/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Event evt)
    {
        if (!_isAdmin) return Forbid();
        if (ModelState.IsValid)
        {
            _context.Add(evt);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(evt);
    }

    // GET: Events/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (!_isAdmin) return Forbid();
        if (id == null) return NotFound();
        var evt = await _context.Events.FindAsync(id);
        if (evt == null) return NotFound();
        return View(evt);
    }

    // POST: Events/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Event evt)
    {
        if (!_isAdmin) return Forbid();
        if (id != evt.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _context.Update(evt);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(evt);
    }

    // GET: Events/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (!_isAdmin) return Forbid();
        if (id == null) return NotFound();
        var evt = await _context.Events.FindAsync(id);
        if (evt == null) return NotFound();
        return View(evt);
    }

    // POST: Events/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!_isAdmin) return Forbid();
        var evt = await _context.Events.FindAsync(id);
        if (evt != null)
        {
            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

}