
namespace SharedResources
{
    public class EncompassSettings
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string InstanceID { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
    }

    public interface IEncompassOptions
    {
        EncompassSettings Encompass { get; set; }
    }
}