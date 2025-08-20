using System.Net.Mail;

namespace mcl959mvc.Classes;

public class EmailTool
{
    private static SmtpSettings _smtpSettings;
    public static async Task SendEmailAsync(
        SmtpSettings settings, 
        string fromName, string fromEmail, string attnTo, string subject, string body)
    {
        _smtpSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        // Implement your email sending logic here
        // For example, use SMTP, SendGrid, or any other provider
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        if (string.IsNullOrEmpty(fromName))
        {
            fromName = _smtpSettings.SiteDomain;
        }
        if (string.IsNullOrEmpty(fromEmail))
        {
            fromEmail = _smtpSettings.FromEmail;
        }
        var mailgunMsg = "<a href=\"http://api.mailgun.net\">Mailgun Requests API</a>";
        var textBody = EmailTextBody(fromName, fromEmail, attnTo, body, mailgunMsg);
        var htmlBody = EmailHtmlBody(fromName, fromEmail, attnTo, body, mailgunMsg);
        var textView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
        var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
        using (var email = new MailMessage()
        {
            Body = textBody,
            From = new MailAddress("info@mcl959.com", "MCL959 Website"),
            IsBodyHtml = false,
            Subject = subject,
        })
        {
            email.To.Add(new MailAddress(settings.FromEmail, "MCL959 Web Sergeant"));
            email.ReplyToList.Add(new MailAddress(fromEmail, fromName));
            email.AlternateViews.Add(textView);
            email.AlternateViews.Add(htmlView);
            using (var smtp = new SmtpClient(settings.Server, 587)
            {
                Credentials = new System.Net.NetworkCredential(settings.Username, settings.Password),
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
    private static String EmailTextBody(String from, String email, String attnTo, String message, String mailgunOpt)
    {
        return @$"
On {DateTime.Now:s}, {from} [{email}] sent the following message to {(string.IsNullOrEmpty(attnTo) ? "" : attnTo)}:

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
    private static String EmailHtmlBody(String from, String email, String attnTo, String message, String mailgunOpt)
    {
        var safeMessage = $"{message}".Replace("\r\n", "<br>").Replace("\n", "<br>");
        String nameAndEmail = @$"{from} <a href='mailto:{email}'>{email}</a>";
        String htmlEmail = $@"
<!DOCTYPE HTML PUBLIC '-//W3C//DTD HTML 4.0 Transitional//EN\'>
<html>
    <head>
        <meta http-equiv='Content-Type' content='text/html; charset=iso-8859-1'>
        <title>Contact Message</title>
        <link rel='stylesheet' type='text/css' href='{_smtpSettings.SiteDomain}/css/email.css'>
        <link rel='shortcut icon' href='{_smtpSettings.SiteLogo}' type='image/x-icon'>
    </head>
    <body>
        <div class='eml'>
            <table>
                <tr>
                    <td>
                        <font>On <i>{DateTime.Now:s}</i>, {nameAndEmail} sent the following message{(string.IsNullOrEmpty(attnTo) ? "" : attnTo)}:</font><br /><br />
                        <table>
                            <tr><td><div><font color='Green'>{safeMessage}</font></div></td></tr>
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
