
namespace SharedResources
{
    public class CreditPackage
    {
        public DenialReasons DenialReasons { get; set; }
        public Business Bureau { get; set; }
        public CreditFileGroup[] CreditFileGroups { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
    }
}
