using System;
using System.Collections.Generic;

namespace SharedResources
{
    public class CreditFile
    {
        public string SequenceNumber { get; set; }
        public string Label { get; set; }
        public DateTime? InfileDate { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
        public string Identifier { get; set; }
        public Business Referrer { get; set; }
        public CreditScore CreditScore { get; set; }
        public CreditScoreModel CreditScoreModel { get; set; }
        public Role Role { get; set; }
        public List<CreditInquiry> CreditInquries { get; set; }
        public List<CreditLiability> CreditLiabilities { get; set; }
    }
}
