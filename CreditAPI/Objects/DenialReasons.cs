namespace SharedResources
{
    public class DenialReasons
    {
        public bool NoCreditFile { get; set; }
        public bool InsufficientCreditReferences { get; set; }
        public bool LimitedCreditExperience { get; set; }
        public bool UnverifiableCreditReferences { get; set; }
        public bool PendingActionOrJudgment { get; set; }
        public bool ExcessiveObligations { get; set; }
        public bool InsufficientIncomeForObligations { get; set; }
        public bool UnacceptablePaymentRecord_PreviousMortgage { get; set; }
        public bool DelinquentCreditObligations { get; set; }
        public bool Bankruptcy { get; set; }
        public bool UnacceptableCreditReferences { get; set; }
        public bool PoorCreditPerformance { get; set; }
        public bool NumberOfRecentInquiries { get; set; }

        public bool UnverifiableEmployment { get; set; }
        public bool LengthOfEmployment { get; set; }
        public bool IrregularEmployment { get; set; }

        public bool InsufficientIncome { get; set; }
        public bool UnverifiableIncome { get; set; }

        public bool TemporaryResidence { get; set; }
        public bool LengthOfResidence { get; set; }
        public bool UnverifiableResidence { get; set; }

        public bool IncompleteApplication { get; set; }
        public bool InadequateCollateral { get; set; }
        public bool UnacceptableProperty { get; set; }
        public bool InsufficientPropertyData { get; set; }
        public bool UnacceptableAppraisal { get; set; }
        public bool UnacceptableLeaseholdEstate { get; set; }
        public bool InsufficientCollateralValue { get; set; }
        public bool TermsDenied { get; set; }
        public bool Custom1 { get; set; }
        public bool Custom2 { get; set; }
    }
}
