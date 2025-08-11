using mcl959mvc;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Controllers;

public class EventsController : Mcl959MemberController
{
    private readonly Mcl959DbContext _context;

    public EventsController(Mcl959DbContext context, UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
        _context = context;
    }

    // GET: Events
    public async Task<IActionResult> Index()
    {
        await CheckUserIdentity();
        return View(await _context.Events.ToListAsync());
    }

    // GET: Events/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var evt = await _context.Events.FindAsync(id);
        if (evt == null) return NotFound();

        var comments = await _context.Comments
            .Where(c => c.TableSource == "Events" && c.ParentId == id)
            .OrderByDescending(c => c.TimeStamp)
            .ToListAsync();

        var model = new EventsAndCommentsModel()
        {
            Event = evt,
            Comments = comments,
        };
        return View(model);
    }

    // GET: Events/Create
    public async Task<IActionResult> Create()
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        return View();
    }

    // POST: Events/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventsModel evt)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
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
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id == null) return NotFound();
        var evt = await _context.Events.FindAsync(id);
        if (evt == null) return NotFound();
        return View(evt);
    }

    // POST: Events/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventsModel evt)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
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
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
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
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        var evt = await _context.Events.FindAsync(id);
        if (evt != null)
        {
            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(CommentsModel item)
    {
        await CheckUserIdentity();
        if (!User.Identity.IsAuthenticated) return Forbid();
        item.TimeStamp = DateTime.UtcNow;
        item.TableSource = "Events";
        _context.Comments.Add(item);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

}