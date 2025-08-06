using Microsoft.Extensions.Configuration;

namespace mcl959mvc.Classes;

public interface IClassKeys
{
    string FacebookAppId { get; }
    string FacebookAppSecret { get; }
    string MailGunHost { get; }
    string MailGunSmtpUser { get; }
    string MailGunSmtpPwd { get; }
    string MicrosoftAppId { get; }
    string MicrosoftAppSecret { get; }
    string TwitterAppId { get; }
    string TwitterAppSecret { get; }
    string LinkedInAppId { get; }
    string LinkedInSecret { get; }
    string GoogleAppId { get; }
    string GoogleAppSecret { get; }
}

public class ClassKeys : IClassKeys
{
    public string FacebookAppId { get; private set; }
    public string FacebookAppSecret { get; private set; }
    public string MailGunHost { get; private set; }
    public string MailGunSmtpUser { get; private set; }
    public string MailGunSmtpPwd { get; private set; }
    public string MicrosoftAppId { get; private set; }
    public string MicrosoftAppSecret { get; private set; }
    public string TwitterAppId { get; private set; }
    public string TwitterAppSecret { get; private set; }
    public string LinkedInAppId { get; private set; }
    public string LinkedInSecret { get; private set; }
    public string GoogleAppId { get; private set; }
    public string GoogleAppSecret { get; private set; }

    public ClassKeys(IConfiguration configuration)
    {
        FacebookAppId = configuration["FacebookAppId"];
        FacebookAppSecret = configuration["FacebookAppSecret"];
        MailGunHost = configuration["MailGunHost"];
        MailGunSmtpUser = configuration["MailGunSmtpUser"];
        MailGunSmtpPwd = configuration["MailGunSmtpPwd"];
        MicrosoftAppId = configuration["MicrosoftAppId"];
        MicrosoftAppSecret = configuration["MicrosoftAppSecret"];
        TwitterAppId = configuration["TwitterAppId"];
        TwitterAppSecret = configuration["TwitterAppSecret"];
        LinkedInAppId = configuration["LinkedInAppId"];
        LinkedInSecret = configuration["LinkedInSecret"];
        GoogleAppId = configuration["GoogleAppId"];
        GoogleAppSecret = configuration["GoogleAppSecret"];
    }
}
