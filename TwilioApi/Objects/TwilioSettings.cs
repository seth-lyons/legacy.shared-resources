
namespace SharedResources
{
    public class TwilioSettings
    {
        public string From { get; set; }
        public string MessagingServiceSid { get; set; }
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
    }

    public interface ITwilioOptions
    {
        TwilioSettings Twilio { get; set; }
    }
}
