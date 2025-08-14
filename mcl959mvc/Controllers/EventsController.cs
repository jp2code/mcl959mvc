using mcl959mvc;
using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace mcl959mvc.Controllers;

public class EventsController : Mcl959MemberController
{
    private readonly Mcl959DbContext _context;
    private readonly SmtpSettings _smtpSettings;

    public EventsController(Mcl959DbContext context, UserManager<ApplicationUser> userManager, IOptions<SmtpSettings> smptOptions, ILogger<Controller> logger)
        : base(userManager, logger)
    {
        _context = context;
        _smtpSettings = smptOptions.Value ?? throw new ArgumentNullException(nameof(smptOptions));
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
        if (id == null)
        {
            _logger.LogWarning("Details called with null id");
            return View("Error404");
        }
        var item = await _context.Events.FindAsync(id);
        if (item == null)
        {
            ViewBag.Id = id;
            return View("Error404");
        }
        var comments = await _context.Comments
            .Where(c => c.TableSource == "Events" && c.ParentId == id)
            .OrderByDescending(c => c.TimeStamp)
            .ToListAsync();

        var model = new EventsAndCommentsModel()
        {
            Event = item,
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
    public async Task<IActionResult> Create(EventsModel item)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (ModelState.IsValid)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(item);
    }

    // GET: Events/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id == null) return NotFound();
        var item = await _context.Events.FindAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    // POST: Events/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventsModel item)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id != item.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
            // Redirect to Details with the same id after saving
            return RedirectToAction(nameof(Details), new { id = item.Id });
        }
        return View(item);
    }

    // GET: Events/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id == null) return NotFound();
        var item = await _context.Events.FindAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    // POST: Events/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        var item = await _context.Events.FindAsync(id);
        if (item != null)
        {
            _context.Events.Remove(item);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(CommentsModel item)
    {
        await CheckUserIdentity();
        if (!IsRegistered) return Forbid();

        item.TimeStamp = DateTime.UtcNow;
        item.TableSource = "Events";
        _context.Comments.Add(item);
        await _context.SaveChangesAsync();
        var regarding = $"{UserEmail}";
        var eventItem = await _context.Events.FindAsync(item.ParentId);
        if (eventItem != null)
        {
            regarding = $"{eventItem.EventName} ({eventItem.Id})";
        }
        var emailMessage = @$"
The following comment was added to the event {regarding} by {UserEmail}:
<blockquote>{item.Message}</blockquote>
";
        await EmailTool.SendEmailAsync(
            _smtpSettings,
            item.UserId, UserEmail, string.Empty,
            $"Comment on Event for {regarding}",
            emailMessage);
        return RedirectToAction(nameof(Details), new { id = item.ParentId });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, int parentId)
    {
        await CheckUserIdentity();
        if (!IsRegistered) return Forbid();
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null) return NotFound();
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        // Redirect back to the event details page
        return RedirectToAction(nameof(Details), new { id = parentId });
    }

}