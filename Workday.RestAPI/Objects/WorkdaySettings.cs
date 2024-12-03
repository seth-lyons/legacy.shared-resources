namespace SharedResources
{
    public class WorkdaySettings
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string RefreshToken { get; set; }
        public string BaseAddress { get; set; }
        public string Tenant { get; set; }
    }

    public interface IWorkdayOptions
    {
        WorkdaySettings Workday { get; set; }
    }
}
