namespace SharedResources
{
    public class ActiveDirectorySettings
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Path { get; set; }
        public string Server { get; set; }
    }

    public interface IActiveDirectoryOptions
    {
        ActiveDirectorySettings ActiveDirectory { get; set; }
    }
}
