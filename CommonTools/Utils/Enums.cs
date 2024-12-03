
namespace SharedResources
{
    public enum EnvironmentType
    {
        Production,
        Stage,
        QA,
        Development
    }
    
    public enum ConflictBehavior
    {
        Exception,
        Overwrite,
        Rename
    }
}
