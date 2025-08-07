using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace mcl959mvc.Classes;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var smtpHost = $"{_config["Smtp:Server"]}";
        var smtpPorts = $"{_config["Smtp:Port"]}";
        var smtpUser = $"{_config["Smtp:Username"]}";
        var smtpPass = $"{_config["Smtp:Password"]}";
        var fromEmail = $"{_config["Smtp:FromEmail"]}";
        var ports = smtpPorts.Split(',');

        using (var client = new SmtpClient(smtpHost, 0)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        })
        {
            var retry = true;
            var mail = new MailMessage(fromEmail, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            };
            foreach (var port in ports)
            {
                int n = 0;
                if (retry && int.TryParse(port, out n))
                {
                    client.Port = n;
                    try
                    {
                        await client.SendMailAsync(mail);
                        retry = false;
                        Console.WriteLine($"Email sent successfully to {email} using port {n}");
                    } catch (Exception err)
                    {
                        Console.WriteLine(err);
                    }
                }
            }
        }
    }
}