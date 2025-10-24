using mcl959mvc;
using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace mcl959mvc.Controllers;

public class MessagesController : Mcl959MemberController
{
    private readonly Mcl959DbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory; // For sending email (or use your own service)
    private readonly SmtpSettings _smtpSettings;
    private static SelectListItem[] LISTITEMSPACERS = new SelectListItem[] {
        new SelectListItem { Value = "admin space", Text = "---- ADMIN OPTION -----" },
        new SelectListItem { Value = "*.*", Text = "All Members" },
        new SelectListItem { Value = "officer space", Text = "--- ELECTED OFFICERS ---" },
        new SelectListItem { Value = "member space", Text = "--- MEMBER SPACE ----" },
    };
    enum ListItemType
    {
        AdminOption,
        AllMembers,
        ElectedOfficers,
        MemberSpace
    }

    public MessagesController(
         IMemoryCache cache,
         IHttpClientFactory httpClientFactory,
         Mcl959DbContext context,
         UserManager<ApplicationUser> userManager,
         IOptions<SmtpSettings> smptOptions,
         ILogger<Controller> logger)
         : base(userManager, logger)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _smtpSettings = smptOptions.Value ?? throw new ArgumentNullException(nameof(smptOptions));
    }

    public async Task<IActionResult> Index()
    {
        await CheckUserIdentity();
        if (IsAdmin)
        {
            return View(await _context.Messages.ToListAsync());
        }
        // Not admin: redirect to Create
        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await CheckUserIdentity();
        var model = await BuildDefaultMessageModelAsync(); // sets defaults and ViewBag.Recipients
        return View(model); // returns Views/Messages/Create.cshtml
    }
    // Helper to detect AJAX (fetch) posts
    private static bool IsAjaxRequest(HttpRequest request) =>
        string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    private List<SelectListItem> GetRecipients()
    {
        var list = new List<SelectListItem>
        {
            new SelectListItem
            {
                Value = "no one",
                Text = "Select a member",
            }
        };
        if (IsAdmin)
        {
            list.Add(LISTITEMSPACERS[(int)ListItemType.AdminOption]);
            list.Add(LISTITEMSPACERS[(int)ListItemType.AllMembers]);
        }
        list.Add(LISTITEMSPACERS[(int)ListItemType.ElectedOfficers]);
        foreach (var item in from rank in _context.MemberRanks
            join member in _context.Roster on rank.MemberNumber equals member.MemberNumber
            where member.DiedOn == null
            orderby rank.NumericRank descending
            select new
            {
                member.PersonalEmail,
                member.PersonalPhone,
                member.WorkEmail,
                member.WorkPhone,
                NameAndRank = $"{member.DisplayName} ({rank.DisplayRank})"
            })
        {
            var email =
                !string.IsNullOrEmpty(item.PersonalEmail) ? item.PersonalEmail :
                !string.IsNullOrEmpty(item.WorkEmail) ? item.WorkEmail :
                !string.IsNullOrEmpty(item.PersonalPhone) ? item.PersonalPhone :
                !string.IsNullOrEmpty(item.WorkPhone) ? item.WorkPhone :
                "[N/A]";
            list.Add(new SelectListItem
            {
                Value = email,
                Text = item.NameAndRank
            });
        }
        if (IsAdmin || IsMember)
        {
            list.Add(LISTITEMSPACERS[(int)ListItemType.MemberSpace]);
            foreach (var item in _context.Roster
                .Where(x => x.DiedOn == null)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName))
            {
                var email = 
                    !string.IsNullOrEmpty(item.PersonalEmail) ? item.PersonalEmail :
                    !string.IsNullOrEmpty(item.WorkEmail) ? item.WorkEmail :
                    !string.IsNullOrEmpty(item.PersonalPhone) ? item.PersonalPhone :
                    !string.IsNullOrEmpty(item.WorkPhone) ? item.WorkPhone :
                    "[N/A]";
                list.Add(new SelectListItem
                {
                    Value = email,
                    Text = item.DisplayName
                });
            }
        }
        var selectedItem = list.FirstOrDefault(x => x.Text == "Select a member");
        if (selectedItem != null)
        {
            selectedItem.Selected = true;
        }
        return list;
    }

    // POST: Messages/Create (adjust to support modal)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MessagesModel item, string? action, IFormFile? Attachment)
    {
        await CheckUserIdentity();
        // Non-registered: same logic, but return Partial for AJAX to keep modal updated
        if (!IsRegistered)
        {
            if (action == "SendCode")
            {
                // Generate and send code
                var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                _cache.Set($"ContactCode_{item.Email}", code, TimeSpan.FromMinutes(10));
                await EmailTool.SendEmailAsync1(
                    _smtpSettings,
                    _smtpSettings.Username, _smtpSettings.FromEmail, $" to {User.Identity?.Name}", "Your verification code", $"Your code is: {code}");
                item.CodeSent = true;
                ModelState.Clear();
                ModelState.AddModelError("Info", "Verification code sent to your email.");
                if (IsAjaxRequest(Request))
                {
                    ViewBag.PopupType = PopupType.Create;
                    ViewBag.Recipients = GetRecipients();
                    return PartialView("_MessagePopup", item);
                }
                ViewBag.Recipients = GetRecipients();
                return View(item);
            }
            else if (action == "SubmitMessage")
            {
                // Validate code
                if (!_cache.TryGetValue($"ContactCode_{item.Email}", out string? code) || code != item.Code)
                {
                    ModelState.AddModelError("Code", "Invalid or expired code.");
                    item.CodeSent = true;
                    if (IsAjaxRequest(Request))
                    {
                        ViewBag.PopupType = PopupType.Create;
                        ViewBag.Recipients = GetRecipients();
                        return PartialView("_MessagePopup", item);
                    }
                    ViewBag.Recipients = GetRecipients();
                    return View(item);
                }
                // Optionally clear the code
                _cache.Remove($"ContactCode_{item.Email}");
            }
            else
            {
                // Initial load or unknown action
                if (IsAjaxRequest(Request))
                {
                    ViewBag.PopupType = PopupType.Create;
                    ViewBag.Recipients = GetRecipients();
                    return PartialView("_MessagePopup", item);
                }
                return View(item);
            }
        }
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest(Request))
            {
                ViewBag.PopupType = PopupType.Create;
                ViewBag.Recipients = GetRecipients();
                return PartialView("_MessagePopup", item);
            }
            ViewBag.Recipients = GetRecipients();
            return View(item);
        } else 
        // If model is valid, save the message
        {
            if (Attachment != null && (0 < Attachment.Length))
            {
                // Validate file size
                if (MAX4MB < Attachment.Length)
                {
                    ModelState.AddModelError("Attachment", "File size exceeds the maximum limit.");
                    return View(item);
                }
                // Validate file type
                var allowedTypes = new[] { ".jpg", ".jpeg", ".gif", ".png", ".pdf", ".doc", ".docx", ".zip" };
                var ext = Path.GetExtension(Attachment.FileName).ToLowerInvariant();
                if (!allowedTypes.Contains(ext))
                {
                    ModelState.AddModelError("Attachment", "Invalid file type.");
                    return View(item);
                }
                // Save file
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Path.GetFileNameWithoutExtension(Attachment.FileName)}.{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Attachment.CopyToAsync(stream);
                }
                var request = HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";
                var fileUrl = $"{baseUrl}/uploads/{fileName.Replace("\\", "/")}";

                // Use fileUrl in your comments
                item.Comments += $"\n\n<b>Attachment:</b> <a href=\"{fileUrl}\">{Attachment.FileName}</a>";
            }
            if (string.IsNullOrEmpty(item.Name))
            {
                item.Name = "John Doe"; // this should never happen if IsValid works correctly
            }
            item.Date = DateTime.UtcNow;
            _context.Messages.Add(item);
            await _context.SaveChangesAsync();
            var fromName = $"{item.Name}";
            var fromEmail = $"{item.Email}";
            var subject = "New Contact Message";
            var attnTo = item.SendTo;
            var emailEveryone = IsAdmin && (attnTo == LISTITEMSPACERS[(int)ListItemType.AllMembers].Value);
            if (!emailEveryone)
            {
                var roster = _context.Roster.FirstOrDefault(x => x.PersonalEmail == attnTo);
                if (roster == null)
                {
                    roster = _context.Roster.FirstOrDefault(x => x.WorkEmail == attnTo);
                }
                if (roster != null)
                {
                    attnTo = $" with attention to {roster.DisplayName} <a href='mailto:{roster.PersonalEmail}'>{roster.PersonalEmail}</a>";
                }
                else if (!string.IsNullOrEmpty(attnTo))
                {
                    attnTo = $" with attention to {attnTo}";
                }
                else
                {
                    attnTo = "";
                }
                var body = $"From: {fromName} <{fromEmail}>\n\n{item.Comments}";
                await EmailTool.SendEmailAsync1(_smtpSettings, fromName, fromEmail, attnTo, subject, body);
            }
            else
            {
                // Email all members
                subject = $"MCL959 Contact Message to All Members";
                var members = from member in _context.Roster
                              where member.DiedOn == null
                              select new
                              {
                                  member.DisplayName,
                                  member.PersonalEmail,
                                  member.WorkEmail
                              };
                foreach (var member in members)
                {
                    if (!string.IsNullOrEmpty(member.PersonalEmail) || !string.IsNullOrEmpty(member.WorkEmail))
                    {
                        var toEmail = !string.IsNullOrEmpty(member.PersonalEmail) ? member.PersonalEmail : member.WorkEmail;
                        await EmailTool.SendEmailAsync2(_smtpSettings, toEmail, subject, item.Comments);
                    }
                }
            }
            ModelState.AddModelError("Success", $"The message below has been sent.<br/>Any replies will be sent to the email address you provided: <a href='mailto:{fromEmail}'>{fromEmail}</a>.");
            return RedirectToAction("Index", "Home");
        }
    }

    // POST: Messages/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MessagesModel message)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id != message.Id) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(message);
                await _context.SaveChangesAsync();
                // Redirect to Details with the same id after saving
                ModelState.AddModelError("Success", $"Message {id} updated successfully.");
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Messages.AnyAsync(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }
        }
        // Repopulate ViewBag for recipients
        ViewBag.Recipients = GetRecipients();
        return View(message);
    }

    // ADD THIS ACTION
    [HttpGet]
    public async Task<IActionResult> Popup(PopupType popupType, int? id)
    {
        await CheckUserIdentity();           // sets IsAdmin on the base controller
        ViewBag.IsAdmin = IsAdmin;           // pass to view

        MessagesModel model;
        switch (popupType)
        {
            case PopupType.Create:
                model = await BuildDefaultMessageModelAsync();
                break;
            default:
                if (id == null) return BadRequest("id is required");
                model = await _context.Messages.FindAsync(id);
                if (model == null) return NotFound();
                break;
        }

        ViewBag.PopupType = popupType;
        if (popupType is PopupType.Create or PopupType.Edit)
            ViewBag.Recipients = GetRecipients();

        return PartialView("_MessagePopup", model);
    }

    // POST: Messages/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        var message = await _context.Messages.FindAsync(id);
        if (message != null)
        {
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    // helper used above (reuses your existing defaults)
    private async Task<MessagesModel> BuildDefaultMessageModelAsync()
    {
        var model = new MessagesModel
        {
            Name = "",
            Email = UserEmail,
            SendTo = "",
            Subject = "MCL959 Contact Message",
            Date = DateTime.Now,
            Code = "",
            CodeSent = false,
            ResetToken = null
        };
        var recipients = GetRecipients();
        ViewBag.Recipients = recipients;
        var selectedRecipient = recipients.FirstOrDefault(x => x.Text == "Select a member");
        if (selectedRecipient != null)
            model.SendTo = selectedRecipient.Value;
        return await Task.FromResult(model);
    }
}