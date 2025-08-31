using mcl959mvc.Classes;
using mcl959mvc.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;

namespace mcl959mvc.Controllers;

public class UnsubscribeController : Controller
{
    private readonly ApplicationDbContext _identityContext;
    private readonly ILogger<UnsubscribeController> _logger;
    private readonly SmtpSettings _smtpSettings;

    public UnsubscribeController(ApplicationDbContext identityContext, SmtpSettings smtpSettings, ILogger<UnsubscribeController> logger)
    {
        _identityContext = identityContext;
        _smtpSettings = smtpSettings;
        _logger = logger;
    }

    [HttpGet("/unsubscribe")]
    public async Task<IActionResult> Index(string email, string token)
    {
        // Validate email
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email is required.");

        // TODO: Remove email from your database or mark as unsubscribed
        bool success = await AddToSuppressionList(email, token);

        if (success)
            return View("Unsubscribed"); // Show confirmation page
        else
            return StatusCode(500, "Unsubscribe failed.");
    }

    public async Task<bool> AddToSuppressionList(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return false;

        var expectedToken = EmailTool.GenerateUnsubscribeToken(email);
        if (token != expectedToken)
        {
            _logger.LogWarning("Invalid unsubscribe token for email {Email}.", email);
            return false;
        }
        var user = await _identityContext.Users.FindAsync(email);
        if (user == null)
        {
            _logger.LogWarning("User with email {Email} not found.", email);
            return false;
        }
        else
        {
            user.Unsubscribe = true; // Example action
            await _identityContext.SaveChangesAsync();
        }
        var response = new HttpResponseMessage();
        using (var client = new HttpClient())
        {
            var array = Encoding.ASCII.GetBytes(_smtpSettings.MailGunApiKey);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(array));
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("address", email)
            });
            response = await client.PostAsync($"https://api.mailgun.net/v3/{_smtpSettings.SiteDomain}/suppression/unsubscribes", content);
        }
        return response.IsSuccessStatusCode;
    }

}
