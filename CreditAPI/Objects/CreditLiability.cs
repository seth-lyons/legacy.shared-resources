using System;
using System.Collections.Generic;

namespace SharedResources
{
    public class CreditLiability
    {
        public string SequenceNumber { get; set; }
        public string Label { get; set; }
        public string AccountIdentifier { get; set; }
        public string OwnershipType { get; set; }
        public string StatusType { get; set; }
        public string AccountType { get; set; }
        public string LoanType { get; set; }
        public string TermsSourceType { get; set; }
        public string TermsDescription { get; set; }
        public decimal? HighBalanceAmount { get; set; }
        public decimal? MonthlyPaymentAmount { get; set; }
        public decimal? PastDueAmount { get; set; }
        public decimal? UnpaidBalanceAmount { get; set; }
        public decimal? CreditLimitAmount { get; set; }
        public decimal? ChargeOffAmount { get; set; }
        public bool? ConsumerDisputeIndicator { get; set; }
        public int? MonthsReviewedCount { get; set; }
        public int? TermsMonthsCount { get; set; }
        public Creditor Creditor { get; set; }
        public DateTime? AccountOpenedDate { get; set; }
        public DateTime? AccountPaidDate { get; set; }
        public DateTime? ReportedDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public DateTime? PaymentPatternStartDate { get; set; }
        public string CreditBusinessType { get; set; }
        public string DetailCreditBusinessType { get; set; }
        public string PaymentPatternDataText { get; set; }
        public CreditRepository[] CreditRepositories { get; set; }
        public CreditComment[] CreditComments { get; set; }

        public CreditRating LiabilityRating { get; set; }
        public CreditRating HighestAdverseRating { get; set; }
        public CreditRating MostRecentAdverseRating { get; set; }
        public CreditRating[] PriorAdverseRatings { get; set; }

        public int? LateCount_30Days { get; set; }
        public int? LateCount_60Days { get; set; }
        public int? LateCount_90Days { get; set; }
    }
}
