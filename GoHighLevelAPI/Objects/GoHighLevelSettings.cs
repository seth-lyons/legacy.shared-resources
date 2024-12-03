
namespace SharedResources
{
    public class GoHighLevelSettings
    {
        public string APIKey { get; set; }
    }

    public interface IGoHighLevelOptions
    {
        GoHighLevelSettings GoHighLevel { get; set; }
    }
}
