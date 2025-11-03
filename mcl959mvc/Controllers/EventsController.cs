using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace mcl959mvc.Controllers;

public class EventsController : Mcl959MemberController
{
    private readonly Mcl959DbContext _context;
    private readonly SmtpSettings _smtpSettings;

    public EventsController(
        Mcl959DbContext context,
        UserManager<ApplicationUser> userManager,
        IOptions<SmtpSettings> smptOptions,
        ILogger<Controller> logger)
        : base(userManager, logger, smptOptions)
    {
        _context = context;
        _smtpSettings = smptOptions.Value ?? throw new ArgumentNullException(nameof(smptOptions));
    }

    // Helper: return the popup partial with correct ViewBag + VM
    private IActionResult EventPartial(PopupType type, EventsModel ev, IEnumerable<CommentsModel>? comments = null)
    {
        ViewBag.PopupType = type;
        if (type is PopupType.Create or PopupType.Edit) BuildImagesViewBag();
        var vm = new EventsAndCommentsModel { Event = ev, Comments = comments?.ToList() ?? new() };
        return PartialView("_EventPopup", vm);
    }

    // Helper: build images list for Create/Edit popups
    private void BuildImagesViewBag()
    {
        var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var imageFiles = Directory.Exists(imagesFolder)
            ? Directory.GetFiles(imagesFolder)
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => Path.GetFileName(f))
                .ToList()
            : new List<string>();
        ViewBag.Images = imageFiles;
    }
    // GET: Events
    public async Task<IActionResult> Index()
    {
        await CheckUserIdentity();
        return View(await _context.Events.ToListAsync());
    }

    // POST: Events/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Event")] EventsModel item, IFormFile? ImageUpload)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        try
        {
            if (ModelState.IsValid)
            {
                if ((ImageUpload != null) && (0 < ImageUpload.Length))
                {
                    if (MAX4MB < ImageUpload.Length)
                    {
                        ModelState.AddModelError("Attachment", "File size exceeds the maximum limit.");
                        return EventPartial(PopupType.Create, item);
                    }
                    var allowedTypes = new[] { ".jpg", ".jpeg", ".gif", ".png" };
                    var ext = Path.GetExtension(ImageUpload.FileName).ToLowerInvariant();
                    if (!allowedTypes.Contains(ext))
                    {
                        ModelState.AddModelError("ImageUpload", "Invalid file type.");
                        return EventPartial(PopupType.Create, item);
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
                await SendEmailAsync(UserEmail, UserEmail, string.Empty,
                    $"Event Created: {item.EventName} ({item.Id})",
                    $"The event '{item.EventName}' (ID: {item.Id}) was CREATED by {UserEmail}.");
                if (IsAjaxRequest(Request))
                {
                    return Json(new { success = true }); // let the client close the modal & reload
                }
                return RedirectToAction(nameof(Index));
            } else
            {
                return EventPartial(PopupType.Create, item);
            }
        } catch (Exception err)
        {
            _logger.LogError(err, "Error creating event");
            ModelState.AddModelError(string.Empty, "Unexpected error creating the event. Please try again.");
        }
        return EventPartial(PopupType.Create, item);
    }

    // POST: Events/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "Event")] int id, EventsModel item, IFormFile? ImageUpload)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id != item.Id)
        {
            ModelState.AddModelError(string.Empty, "Mismatched event id.");
            return EventPartial(PopupType.Edit, item);
        }
        if (ModelState.IsValid)
        {
            if ((ImageUpload != null) && (0 < ImageUpload.Length))
            {
                if (MAX4MB < ImageUpload.Length)
                {
                    ModelState.AddModelError("Attachment", "File size exceeds the maximum limit.");
                    return EventPartial(PopupType.Edit, item);
                }
                var allowedTypes = new[] { ".jpg", ".jpeg", ".gif", ".png" };
                var ext = Path.GetExtension(ImageUpload.FileName).ToLowerInvariant();
                if (!allowedTypes.Contains(ext))
                {
                    ModelState.AddModelError("ImageUpload", "Invalid file type.");
                    return EventPartial(PopupType.Edit, item);
                }
                var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                using var image = SixLabors.ImageSharp.Image.Load(ImageUpload.OpenReadStream());
                image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(600, 600) }));
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageUpload.FileName)}";
                var filePath = Path.Combine(imagesFolder, fileName);
                await image.SaveAsync(filePath);
                item.ImageFileName = fileName;
            }
            _context.Update(item);
            await _context.SaveChangesAsync();
            await SendEmailAsync(UserEmail, UserEmail, string.Empty,
                $"Event Edited: {item.EventName} ({item.Id})",
                $"The event '{item.EventName}' (ID: {item.Id}) was EDITED by {UserEmail}.");
            if (IsAjaxRequest(Request))
            {
                return Json(new { success = true }); // let the client close the modal & reload
            }
            return RedirectToAction(nameof(Index));
        } else
        {
            return EventPartial(PopupType.Edit, item);
        }
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
            if (IsAjaxRequest(Request))
            {
                return Json(new { success = true }); // let the client close the modal & reload
            }
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
        await SendEmailAsync(
            item.UserId, UserEmail, string.Empty,
            $"Comment on Event for {regarding}",
            emailMessage);
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, int parentId)
    {
        await CheckUserIdentity();
        if (!IsRegistered) return Forbid();
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null) return NotFound($"Comment with ID {id} not found.");
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        // Redirect back to the event details page
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Popup(PopupType popupType, int? id)
    {
        await CheckUserIdentity();           // sets IsAdmin on the base controller
        ViewBag.IsAdmin = IsAdmin;           // pass to view

        if (popupType is PopupType.Create || popupType is PopupType.Edit)
        {
            // Build images list (same code you already use)
            var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var imageFiles = Directory.Exists(imagesFolder)
                ? Directory.GetFiles(imagesFolder)
                    .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Select(f => Path.GetFileName(f))
                    .ToList()
                : new List<string>();
            ViewBag.Images = imageFiles;
        }

        EventsModel model;
        switch (popupType)
        {
            case PopupType.Create:
                await CheckUserIdentity();
                if (!IsAdmin) return Forbid();
                model = new EventsModel();
                break;
            default:
                if (id == null) return BadRequest("id is required");
                var item = await _context.Events.FindAsync(id);
                if (item == null) return NotFound($"Event with ID {id} not found.");
                model = item;
                break;
        }

        ViewBag.PopupType = popupType;
        var comments = (popupType != PopupType.Create && model.Id != 0)
            ? await _context.Comments
                .Where(c => c.TableSource == "Events" && c.ParentId == model.Id)
                .OrderByDescending(c => c.TimeStamp)
                .ToListAsync()
            : new List<CommentsModel>();
        var modelWithComments = new EventsAndCommentsModel
        {
            Event = model,
            Comments = comments
        };
        return PartialView("_EventPopup", modelWithComments);
    }
}