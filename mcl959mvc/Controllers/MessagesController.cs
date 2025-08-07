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

namespace mcl959mvc.Controllers;

public class MessagesController : Controller
{
    private const int MAX4MB = 4 * 1024 * 1024; // 4 MB
    private readonly Mcl959DbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory; // For sending email (or use your own service)
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SmtpSettings _smtpSettings;

    // These are used to determine if the user is an admin or registered
    private bool _isAdmin = false;
    private bool _isRegistered = false;

    public MessagesController(
         IMemoryCache cache,
         IHttpClientFactory httpClientFactory,
         Mcl959DbContext context,
         UserManager<ApplicationUser> userManager,
         IOptions<SmtpSettings> smptOptions)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _smtpSettings = smptOptions.Value ?? throw new ArgumentNullException(nameof(smptOptions));
        _userManager = userManager;
        _isRegistered = User.HasClaim("isRegistered", "true");
        _isAdmin = User.HasClaim("isAdmin", "true");
    }

    public async Task<IActionResult> Index()
    {
        if (_isAdmin)
        {
            return View(await _context.Messages.ToListAsync());
        }
        // Not admin: redirect to Create
        return RedirectToAction(nameof(Create));
    }

    // GET: Messages/Create
    public IActionResult Create()
    {
        var list = new List<SelectListItem>();
        list.Add(new SelectListItem
        {
            Value = string.Empty,
            Text = "Select a member"
        });
        if (!_isAdmin)
        {
            foreach (var item in from rank in _context.MemberRanks
                                 join member in _context.Roster on rank.MemberNumber equals member.MemberNumber
                                 orderby rank.DisplayRank
                                 select new
                                 {
                                     member.PersonalEmail,
                                     member.DisplayName
                                 })
            {
                list.Add(new SelectListItem
                {
                    Value = item.PersonalEmail,
                    Text = item.DisplayName
                });
            }
        }
        else
        {
            foreach (var item in _context.Roster)
            {
                list.Add(new SelectListItem
                {
                    Value = item.PersonalEmail,
                    Text = item.DisplayName
                });
            }
        }
        ViewBag.IsRegistered = _isRegistered;
        ViewBag.IsAdmin = _isAdmin;
        ViewBag.Recipients = list.ToArray();
        return View(new Message
        {
            Name = string.Empty,
            Email = string.Empty,
            Subject = "MCL959 Contact Message",
            Date = DateTime.UtcNow,
            CodeSent = false,
            ResetToken = null,
            Code = string.Empty
        });
    }

    // POST: Messages/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Message item, string? action, IFormFile? Attachment)
    {
        // If not registered, handle code verification
        if (!_isRegistered)
        {
            if (action == "SendCode")
            {
                // Generate and send code
                var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                _cache.Set($"ContactCode_{item.Email}", code, TimeSpan.FromMinutes(10));
                await SendEmailAsync(_smtpSettings.Username, _smtpSettings.FromEmail, $"{User.Identity?.Name}", "Your verification code", $"Your code is: {code}");
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
            string? uploadLink = null;
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
                var fileName = $"{Attachment.FileName}.{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Attachment.CopyToAsync(stream);
                }
                uploadLink = $"/uploads/{fileName}";
                item.Comments += $"\n\n<b>Attachment:</b> <a href=\"{uploadLink}\">{Attachment.FileName}</a>";
            }
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (_isRegistered && user != null)
            {
                item.Name = user.UserName;
                item.Email = user.Email ?? string.Empty;
            }
            item.Date = DateTime.UtcNow;
            _context.Messages.Add(item);
            await _context.SaveChangesAsync();
            var fromName = $"{item.Name}";
            var fromEmail = $"{item.Email}";
            var subject = "New Contact Message";
            var attnTo = item.SendTo;
            var roster = _context.Roster.FirstOrDefault(x => x.DisplayName == attnTo);
            if (roster != null)
            {
                attnTo = $"{roster.DisplayName} <{roster.PersonalEmail}>";
            }
            var body = $"From: {fromName} <{fromEmail}>\n\n{item.Comments}";
            await SendEmailAsync(fromName, fromEmail, attnTo, subject, body);

            ModelState.AddModelError("Success", "Your message has been sent.");
            return RedirectToAction(nameof(Details), new { id = item.Id });
        }
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
        if (!_isAdmin) return Forbid();
        if (id == null) return NotFound();
        var message = await _context.Messages.FindAsync(id);
        if (message == null) return NotFound();
        return View(message);
    }

    // POST: Messages/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Message message)
    {
        if (!_isAdmin) return Forbid();
        if (id != message.Id) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(message);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Messages.AnyAsync(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }
        }
        return View(message);
    }

    // GET: Messages/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (!_isAdmin) return Forbid();
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
        if (!_isAdmin) return Forbid();
        var message = await _context.Messages.FindAsync(id);
        if (message != null)
        {
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task SendEmailAsync(string fromName, string fromEmail, string attnTo, string subject, string body)
    {
        // Implement your email sending logic here
        // For example, use SMTP, SendGrid, or any other provider
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        var mailgunMsg = "<a href=\"http://api.mailgun.net\">Mailgun Requests API</a>";
        var textBody = EmailTextBody(fromName, fromEmail, attnTo, body, mailgunMsg);
        var htmlBody = EmailHtmlBody(fromName, fromEmail, attnTo, body, mailgunMsg);
        var mimeType = new System.Net.Mime.ContentType("text/html");
        var alternate = new AlternateView(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(textBody)), mimeType);
        using (var email = new MailMessage()
        {
            Body = htmlBody,
            BodyEncoding = System.Text.Encoding.UTF8,
            BodyTransferEncoding = System.Net.Mime.TransferEncoding.Base64,
            DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure,
            IsBodyHtml = true,
            Subject = subject,
        })
        {
            email.From = new MailAddress(_smtpSettings.FromEmail); // IMPORTANT: This must be the same as your SMTP authentication address
            email.To.Add(new MailAddress(_smtpSettings.FromEmail, "MCL959 Contact Page"));
            email.AlternateViews.Add(alternate);
            using (var smtp = new SmtpClient(_smtpSettings.Server, 587)
            {
                DeliveryFormat = SmtpDeliveryFormat.International,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true,
                UseDefaultCredentials = false,
            })
            {
                smtp.Credentials = new System.Net.NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                await smtp.SendMailAsync(email);
            }
        }
    }
    /// <summary>
    /// Creates the email body
    /// </summary>
    /// <param name="from">String - name of sender</param>
    /// <param name="email">String - email of sender</param>
    /// <param name="message">String - email message</param>
    /// <param name="mailgunOpt">String - mailgun parameter</param>
    /// <returns>String email body</returns>
    private String EmailTextBody(String from, String email, String attnTo, String message, String mailgunOpt)
    {
        return @$"
On {DateTime.Now:s}, {from} [{email}] sent the following message to {attnTo}:

    {message}

End of Message

{mailgunOpt}
(Text View)";
    }
    /// <summary>
    /// Creates the HTML email body
    /// </summary>
    /// <param name="from">String - name of sender</param>
    /// <param name="email">String - email of sender</param>
    /// <param name="message">String - email message</param>
    /// <param name="mailgunOpt">String - mailgun parameter</param>
    /// <returns>String HTML email body</returns>
    private String EmailHtmlBody(String from, String email, String attnTo, String message, String mailgunOpt)
    {
        String nameAndEmail = @$"{from} <a href='{email}'>{1}</a>";
        String htmlEmail = $@"
<!DOCTYPE HTML PUBLIC '-//W3C//DTD HTML 4.0 Transitional//EN\'>
<html>
    <head>
        <meta http-equiv='Content-Type' content='text/html; charset=iso-8859-1'>
        <title>Contact Message</title>
        <style>
            body {{ background-color: #cccccc; }}
            .eml {{ font-family: Arial, sans-serif; color: #ff0000; }}
            .epl {{ text-align: center; }}
            .eml td {{ padding: 10px; }}
            .epl img {{ height: 150px; width: 150px; }}
        </style>
        <link rel='stylesheet' type='text/css' href='{_smtpSettings.SiteDomain}/css/email.css'>
        <link rel='shortcut icon' href='{_smtpSettings.SiteLogo}' type='image/x-icon'>
    </head>
    <body>
    <pre>
        <a href='{_smtpSettings.SiteDomain}'>{_smtpSettings.SiteDomain}</a>
    </pre>
    <pre>
        <div class='eml'>
            <table>
                <tr>
                    <td>
                        <font>On <i>{DateTime.Now:s}</i>, <b>{nameAndEmail}</b> sent the following message to <b>{attnTo}</b>:</font><br /><br />
                        <table>
                            <tr><td><font color='Green'>{message}</font></td></tr>
                            <tr><td>&nbsp;</td></tr>
                            <tr><td><br><font>End of Message.</font></td></tr>
                        </table>
                    </td>
                    <td class='epl'><img src='{_smtpSettings.SiteLogo}' alt='{_smtpSettings.Server}' style='height:150px; width:150px'></td>
                </tr>
            </table>
        </div>
        <div class='eml'>
            <p><font size='1'>This message was sent from <a href='{_smtpSettings.SiteDomain}' target='_blank'>{_smtpSettings.SiteDomain}</a> on behalf of <b>{from}</b>.<hr/>{mailgunOpt}</font></p>
        </div>
        </body>
</html>";
        return htmlEmail;
    }

}