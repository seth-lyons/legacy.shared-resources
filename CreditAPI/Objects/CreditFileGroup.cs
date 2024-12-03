using System.Collections.Generic;

namespace SharedResources
{
    public class CreditFileGroup
    {
        public string[] Identifiers { get; set; }

        public List<CreditFile> CreditFiles { get; set; }
    }
}
