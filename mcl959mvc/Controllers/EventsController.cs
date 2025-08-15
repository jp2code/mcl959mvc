using mcl959mvc;
using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;

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
        // Get list of image files from wwwroot/images
        var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var imageFiles = Directory.Exists(imagesFolder)
            ? Directory.GetFiles(imagesFolder)
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => Path.GetFileName(f))
                .ToList()
            : new List<string>();
        ViewBag.Images = imageFiles;
        return View(new EventsModel());
    }

    // POST: Events/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventsModel item, IFormFile? ImageUpload)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (ModelState.IsValid)
        {
            if ((ImageUpload != null) && (0 < ImageUpload.Length))
            {
                if (MAX4MB < ImageUpload.Length)
                {
                    ModelState.AddModelError("Attachment", "File size exceeds the maximum limit.");
                    return View(item);
                }
                var allowedTypes = new[] { ".jpg", ".jpeg", ".gif", ".png" };
                var ext = Path.GetExtension(ImageUpload.FileName).ToLowerInvariant();
                if (!allowedTypes.Contains(ext))
                {
                    ModelState.AddModelError("ImageUpload", "Invalid file type.");
                    return View(item);
                }
                var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                using var image = Image.Load(ImageUpload.OpenReadStream());
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(600, 600) // Adjust as needed for 30% width
                }));
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageUpload.FileName)}";
                var filePath = Path.Combine(imagesFolder, fileName);
                await image.SaveAsync(filePath);
                item.ImageFileName = fileName;
            }
            item.EventCreated = DateTime.UtcNow;
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
        // Get list of image files from wwwroot/images
        var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var imageFiles = Directory.Exists(imagesFolder)
            ? Directory.GetFiles(imagesFolder)
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => Path.GetFileName(f))
                .ToList()
            : new List<string>();
        ViewBag.Images = imageFiles;
        return View(item);
    }

    // POST: Events/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventsModel item, IFormFile? ImageUpload)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id != item.Id) return NotFound();
        if (ModelState.IsValid)
        {
            if ((ImageUpload != null) && (0 < ImageUpload.Length))
            {
                if (MAX4MB < ImageUpload.Length)
                {
                    ModelState.AddModelError("Attachment", "File size exceeds the maximum limit.");
                    return View(item);
                }
                var allowedTypes = new[] { ".jpg", ".jpeg", ".gif", ".png" };
                var ext = Path.GetExtension(ImageUpload.FileName).ToLowerInvariant();
                if (!allowedTypes.Contains(ext))
                {
                    ModelState.AddModelError("ImageUpload", "Invalid file type.");
                    return View(item);
                }
                var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                using var image = Image.Load(ImageUpload.OpenReadStream());
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(600, 600) // Adjust as needed for 30% width
                }));
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageUpload.FileName)}";
                var filePath = Path.Combine(imagesFolder, fileName);
                await image.SaveAsync(filePath);
                item.ImageFileName = fileName;
            }
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