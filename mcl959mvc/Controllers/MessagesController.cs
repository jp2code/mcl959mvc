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
         IOptions<SmtpSettings> smptOptions)
         : base(userManager)
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
        var list = new List<SelectListItem>();
        list.Add(new SelectListItem
        {
            Value = string.Empty,
            Text = "Select a member",
            Selected = true
        });
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
        ViewBag.Recipients = list.ToArray();
        return View(new Message
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

    // POST: Messages/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Message item, string? action, IFormFile? Attachment)
    {
        await CheckUserIdentity();
        if (!IsRegistered)
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
                var fileName = $"{Path.GetFileNameWithoutExtension(Attachment.FileName)}.{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Attachment.CopyToAsync(stream);
                }
                item.Comments += $"\n\n<b>Attachment:</b> <a href=\"{filePath}\">{Attachment.FileName}</a>";
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
    public async Task<IActionResult> Edit(int id, Message message)
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

    private async Task SendEmailAsync(string fromName, string fromEmail, string attnTo, string subject, string body)
    {
        // Implement your email sending logic here
        // For example, use SMTP, SendGrid, or any other provider
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        var mailgunMsg = "<a href=\"http://api.mailgun.net\">Mailgun Requests API</a>";
        var textBody = EmailTextBody(fromName, fromEmail, attnTo, body, mailgunMsg);
        var htmlBody = EmailHtmlBody(fromName, fromEmail, attnTo, body, mailgunMsg);
        var textView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
        var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
        using (var email = new MailMessage()
        {
            Body = textBody,
            From = new MailAddress(_smtpSettings.FromEmail, "MCL959 Contact Page"),
            IsBodyHtml = false,
            Subject = subject,
        })
        {
            email.To.Add(new MailAddress(_smtpSettings.FromEmail, "MCL959 Contact Page"));
            email.AlternateViews.Add(textView);
            email.AlternateViews.Add(htmlView);
            using (var smtp = new SmtpClient(_smtpSettings.Server, 587)
            {
                Credentials = new System.Net.NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                DeliveryFormat = SmtpDeliveryFormat.International,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true,
                UseDefaultCredentials = false,
            })
            {
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