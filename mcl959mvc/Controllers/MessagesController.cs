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
    private const int MAX4MB = 4 * 1024 * 1024; // 4 MB
    private readonly Mcl959DbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory; // For sending email (or use your own service)
    private readonly SmtpSettings _smtpSettings;

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

    // GET: Messages/Create
    public async Task<IActionResult> Create()
    {
        await CheckUserIdentity();
        ViewBag.Recipients = GetRecipients();
        return View(new MessagesModel
        {
            Name = string.Empty,
            Email = UserEmail,
            Subject = "MCL959 Contact Message",
            SendTo = string.Empty,
            Date = DateTime.UtcNow,
            CodeSent = false,
            ResetToken = null,
            Code = string.Empty
        });
    }

    private List<SelectListItem> GetRecipients()
    {
        var list = new List<SelectListItem>
        {
            new SelectListItem
            {
                Value = string.Empty,
                Text = "Select a member",
                Selected = true
            }
        };
        if (!IsAdmin)
        {
            foreach (var item in from rank in _context.MemberRanks
                                 join member in _context.Roster on rank.MemberNumber equals member.MemberNumber
                                 where member.DiedOn == null
                                 orderby rank.DisplayRank
                                 select new
                                 {
                                     member.PersonalEmail,
                                     NameAndRank = $"{member.DisplayName} ({rank.DisplayRank})"
                                 })
            {
                list.Add(new SelectListItem
                {
                    Value = item.PersonalEmail,
                    Text = item.NameAndRank
                });
            }
        }
        else
        {
            foreach (var item in _context.Roster
                .Where(x => x.DiedOn == null)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName))
            {
                var email = !string.IsNullOrEmpty(item.PersonalEmail) ? item.PersonalEmail :
                    !string.IsNullOrEmpty(item.WorkEmail) ? item.WorkEmail :
                    !string.IsNullOrEmpty(item.PersonalPhone) ? item.PersonalPhone :
                    !string.IsNullOrEmpty(item.WorkPhone) ? item.WorkPhone :
                    $"[NO Info For '{item.DisplayName}']";
                list.Add(new SelectListItem
                {
                    Value = email,
                    Text = item.DisplayName
                });
            }
        }
        return list;
    }

    // POST: Messages/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MessagesModel item, string? action, IFormFile? Attachment)
    {
        await CheckUserIdentity();
        if (!IsRegistered)
        {
            if (action == "SendCode")
            {
                // Generate and send code
                var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                _cache.Set($"ContactCode_{item.Email}", code, TimeSpan.FromMinutes(10));
                await EmailTool.SendEmailAsync(
                    _smtpSettings,
                    _smtpSettings.Username, _smtpSettings.FromEmail, $" to {User.Identity?.Name}", "Your verification code", $"Your code is: {code}");
                item.CodeSent = true;
                ModelState.Clear();
                ModelState.AddModelError("Info", "Verification code sent to your email.");
                return View(item);
            }
            else if (action == "SubmitMessage")
            {
                // Validate code
                if (!_cache.TryGetValue($"ContactCode_{item.Email}", out string? code) || code != item.Code)
                {
                    ModelState.AddModelError("Code", "Invalid or expired code.");
                    item.CodeSent = true;
                    return View(item);
                }
                // Optionally clear the code
                _cache.Remove($"ContactCode_{item.Email}");
            }
            else
            {
                // Initial load or unknown action
                return View(item);
            }
        }
        // If model is valid, save the message
        if (ModelState.IsValid)
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
            await CheckUserIdentity();
            if (string.IsNullOrEmpty(item.Name))
            {
                item.Name = "John Doe";
            }
            item.Date = DateTime.UtcNow;
            _context.Messages.Add(item);
            await _context.SaveChangesAsync();
            var fromName = $"{item.Name}";
            var fromEmail = $"{item.Email}";
            var subject = "New Contact Message";
            var attnTo = item.SendTo;
            var roster = _context.Roster.FirstOrDefault(x => x.PersonalEmail == attnTo);
            if (roster == null)
            {
                roster = _context.Roster.FirstOrDefault(x => x.WorkEmail == attnTo);
            }
            if (roster != null)
            {
                attnTo = $" with attention to {roster.DisplayName} <a href='mailto:{roster.PersonalEmail}'>{roster.PersonalEmail}</a>";
            } else if (!string.IsNullOrEmpty(attnTo))
            {
                attnTo = $" with attention to {attnTo}";
            } else
            {
                attnTo = "";
            }
            var body = $"From: {fromName} <{fromEmail}>\n\n{item.Comments}";
            await EmailTool.SendEmailAsync(_smtpSettings, fromName, fromEmail, attnTo, subject, body);
            TempData["EmailSentMessage"] = $"The message below has been sent.<br/>Any replies will be sent to the email address you provided: <a href='mailto:{fromEmail}'>{fromEmail}</a>.";
            ModelState.AddModelError("Success", "Your message has been sent.");
            return RedirectToAction(nameof(Details), new { id = item.Id });
        }
        // Repopulate ViewBag for recipients
        ViewBag.Recipients = GetRecipients();
        return View(item);
    }

    // GET: Messages/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var message = await _context.Messages.FindAsync(id);
        if (message == null) return NotFound();
        return View(message);
    }

    // GET: Messages/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id == null) return NotFound();
        var message = await _context.Messages.FindAsync(id);
        if (message == null) return NotFound();
        return View(message);
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
                return RedirectToAction(nameof(Details), new { id = message.Id });
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

    // GET: Messages/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id == null) return NotFound();
        var message = await _context.Messages.FindAsync(id);
        if (message == null) return NotFound();
        return View(message);
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

}