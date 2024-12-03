using System;
using System.Collections.Generic;

namespace SharedResources
{
    public class CreditScore
    {
        public string SequenceNumber { get; set; }
        public string Label { get; set; }
        public DateTime? Date { get; set; }
        public bool FACTAInquiriesIndicator { get; set; }
        public string CreditScoreRankPercentile { get; set; }
        public string CreditScoreValue { get; set; }
        public List<CreditScoreFactor> Factors { get; set; }
    }
}
