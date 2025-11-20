using System.Threading.Tasks;
using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace mcl959mvc.Services
{
    public interface IChatAuditService
    {
        Task LogAndEmailAsync(string? userEmail, string question, string answer, bool isRegistrationHelp);
    }

    public class ChatAuditService : IChatAuditService
    {
        private readonly Mcl959DbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ChatAuditService> _logger;
        private readonly SmtpSettings _smtp;

        public ChatAuditService(
            Mcl959DbContext db,
            IEmailSender emailSender,
            IOptions<SmtpSettings> smtpOptions,
            ILogger<ChatAuditService> logger)
        {
            _db = db;
            _emailSender = emailSender;
            _logger = logger;
            _smtp = smtpOptions.Value;
        }

        public async Task LogAndEmailAsync(string? userEmail, string question, string answer, bool isRegistrationHelp)
        {
            var entry = new ChatLog
            {
                UserEmail = userEmail,
                Question = question,
                Answer = answer,
                IsRegistrationHelp = isRegistrationHelp
            };

            _db.ChatLogs.Add(entry);
            await _db.SaveChangesAsync();

            var subject = $"MCL959: Chat Q/A Logged ({(isRegistrationHelp ? "Registration" : "General")})";
            var body = $@"
<h4>Chat Question Logged</h4>
<table style=""border-collapse:collapse;"">
<tr><td style=""padding:4px 8px;font-weight:bold;"">User</td><td style=""padding:4px 8px;"">{(string.IsNullOrEmpty(userEmail) ? "(anonymous/authenticated user missing email)" : userEmail)}</td></tr>
<tr><td style=""padding:4px 8px;font-weight:bold;"">Time (UTC)</td><td style=""padding:4px 8px;"">{entry.TimeStamp:yyyy-MM-dd HH:mm:ss}</td></tr>
<tr><td style=""padding:4px 8px;font-weight:bold;"">Registration Help?</td><td style=""padding:4px 8px;"">{(isRegistrationHelp ? "Yes" : "No")}</td></tr>
<tr><td style=""padding:4px 8px;font-weight:bold;"">Question</td><td style=""padding:4px 8px;"">{System.Net.WebUtility.HtmlEncode(question)}</td></tr>
<tr><td style=""padding:4px 8px;font-weight:bold;"">Answer</td><td style=""padding:4px 8px;"">{answer}</td></tr>
</table>";
            try
            {
                // Send to site email only (or extend to admins later)
                await _emailSender.SendEmailAsync(_smtp.FromEmail, subject, body);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send chat audit email.");
            }
        }
    }
}