using System.Collections.Generic;
using System.Linq;

namespace SharedResources
{
    public class BorrowerPairList
    {
        private BorrowerPair _activeBP { get; set; }
        public BorrowerPair this[int i]
        {
            get
            {
                if (_activeBP?.BorrowerPairNumber != i)
                    _activeBP = BorrowerPairs?.FirstOrDefault(bp => bp.BorrowerPairNumber == i);
                return _activeBP;
            }
        }
        public List<BorrowerPair> BorrowerPairs { get; set; }
    }

    public class BorrowerPair
    {
        public int BorrowerPairNumber { get; set; }
        public string BorrowerPairID { get; set; }        
        public string CreditReferenceNumber { get; set; }
        public Person Borrower { get; set; }
        public Person Coborrower { get; set; }
    }
}
