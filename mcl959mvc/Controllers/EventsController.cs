using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace mcl959mvc.Controllers;

public class EventsController : Mcl959MemberController
{
    private readonly Mcl959DbContext _context;
    private readonly IMemoryCache _cache;
    private readonly SmtpSettings _smtpSettings;

    public EventsController(
        Mcl959DbContext context,
        UserManager<ApplicationUser> userManager,
        IOptions<SmtpSettings> smptOptions,
        ILogger<Controller> logger,
        IMemoryCache cache)
        : base(userManager, logger, smptOptions)
    {
        _context = context;
        _smtpSettings = smptOptions.Value ?? throw new ArgumentNullException(nameof(smptOptions));
        _cache = cache;
    }

    // Helper: return the popup partial with correct ViewBag + VM
    private IActionResult EventPartial(PopupType type, EventsModel ev, IEnumerable<CommentsModel>? comments = null)
    {
        ViewBag.PopupType = type;
        if (type is PopupType.Create or PopupType.Edit) BuildImagesViewBag();
        var vm = new EventsAndCommentsModel { Event = ev, Comments = comments?.ToList() ?? new() };
        return PartialView("_EventPopup", vm);
    }
    // helper to rebuild the event + comments VM
    private async Task<EventsAndCommentsModel?> BuildEventVmAsync(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return null;
        var comments = await _context.Comments
            .Where(c => c.TableSource == "Events" && c.ParentId == ev.Id)
            .OrderByDescending(c => c.TimeStamp)
            .ToListAsync();
        return new EventsAndCommentsModel { Event = ev, Comments = comments };
    }    // Helper: build images list for Create/Edit popups
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
    [HttpGet]
    [Route("Events/{id:int?}", Name = "EventById")]
    [Route("Events")]
    [Route("Events/Index")]
    [Route("Events/Index/{id:int?}")]
    public async Task<IActionResult> Index(int? id)
    {
        await CheckUserIdentity();

        var list = await _context.Events.ToListAsync();

        if (id.HasValue)
        {
            if (list.Any(e => e.Id == id.Value))
            {
                SetAutoOpenPopup("Events", "Details", id.Value);
            }
            else
            {
                TempData["PopupMessage"] = $"Event {id.Value} was not found.";
            }
        }

        return View(list);
    }

    // POST: Events/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Event")] EventsModel item, IFormFile? ImageUpload)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        // Idempotency: prevent duplicate submission (10 min scope)
        var submissionId = Request.Form["SubmissionId"].ToString();
        if (!string.IsNullOrWhiteSpace(submissionId))
        {
            var key = $"evt_sub_{submissionId}";
            if (_cache.TryGetValue(key, out _))
            {
                if (IsAjaxRequest(Request))
                    return Json(new { success = true }); // silently ignore duplicate
                return RedirectToAction(nameof(Index));
            }
            _cache.Set(key, true, TimeSpan.FromMinutes(10));
        }

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
                var notifyPublic = true;
#if DEBUG
                notifyPublic = false;
#endif
                await _context.SaveChangesAsync();
                var eventUrl = $"{Url.RouteUrl("EventById", new { id = item.Id }, Request.Scheme)}";
                await SendEmailAsync(UserEmail,
                    $"MCL959 Event Created: {item.EventName}",
                    $@"
The event <a href=\""{ eventUrl}\"">'{item.EventName}'</a> was CREATED by {UserEmail}.

Visit {eventUrl} for details.
", notifyPublic);
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
            var eventUrl = $"{Url.RouteUrl("EventById", new { id = item.Id }, Request.Scheme)}";
            await SendEmailAsync(UserEmail,
                $"MCL959 Event Edited: {item.EventName}",
                $@"
The event <a href=\""{eventUrl}\"">'{item.EventName}'</a> was EDITED by {UserEmail}.

Visit {eventUrl} for details.
", false);
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

        // Idempotency guard
        var submissionId = Request.Form["SubmissionId"].ToString();
        if (IsRegistered && !string.IsNullOrWhiteSpace(submissionId))
        {
            var key = $"evt_cmt_{submissionId}";
            if (!_cache.TryGetValue(key, out _))
            {
                _cache.Set(key, true, TimeSpan.FromMinutes(10));
                // Duplicate recent comment guard (same user/message within 5s)
                var trimmedMsg = (item.Message ?? "").Trim();
                var nowUtc = DateTime.UtcNow;
                var recentDuplicate = await _context.Comments.AnyAsync(c =>
                    c.TableSource == "Events" &&
                    c.ParentId == item.ParentId &&
                    c.UserId == item.UserId &&
                    c.Message == trimmedMsg &&
                    EF.Functions.DateDiffSecond(c.TimeStamp, nowUtc) < 5);

                if (!recentDuplicate)
                {
                    item.TimeStamp = nowUtc;
                    item.TableSource = "Events";
                    item.Message = trimmedMsg;
                    _context.Comments.Add(item);
                    await _context.SaveChangesAsync();

                    // Optional notification email (do after save)
                    var eventUrl = $"{Url.RouteUrl("EventById", new { id = item.ParentId }, Request.Scheme)}";
                    var eventName = item.ParentId.ToString();
                    var eventItem = await _context.Events.FindAsync(item.ParentId);
                    if (eventItem != null)
                    {
                        eventName = $"{eventItem.EventName}";
                    }
                    await SendEmailAsync(UserEmail,
                        $"MCL959 Event Comment: {eventName}",
                        $@"
The following comment was added to the event <a href=\""{eventUrl}\"">'{eventName}'</a> by {UserEmail}.

Visit {eventUrl} for details.
", false);
                }
            }
        }
        var vm = await BuildEventVmAsync(item.ParentId);
        if (vm == null) return NotFound();
        ViewBag.PopupType = PopupType.Details;
        return PartialView("_EventPopup", vm);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, int parentId)
    {
        await CheckUserIdentity();
        ViewBag.PopupType = PopupType.Details;
        // Idempotency
        var submissionId = Request.Form["SubmissionId"].ToString();
        if (!string.IsNullOrWhiteSpace(submissionId))
        {
            var key = $"evt_cmt_del_{submissionId}";
            if (_cache.TryGetValue(key, out _))
            {
                var vmCached = await BuildEventVmAsync(parentId);
                if (vmCached != null)
                {
                    return PartialView("_EventPopup", vmCached);
                }
            }
            _cache.Set(key, true, TimeSpan.FromMinutes(10));
        }
        // Direct SQL-style deletion (safe is already gone)
#if NET8_0_OR_GREATER
        var rows = await _context.Comments
            .Where(c => c.Id == id && c.TableSource == "Events" && c.ParentId == parentId)
            .ExecuteDeleteAsync();
#else
        // Fallback for earlier versions
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == id && c.TableSource == "Events" && c.ParentId == parentId);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            try {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Ignore: another request already deleted it
            }
        }
#endif
        var vm = await BuildEventVmAsync(parentId);
        if (IsAjaxRequest(Request))
        {
            return PartialView("_EventPopup", vm);
        }
        else
        {
            return RedirectToAction("Index", new { id = parentId });
        }
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