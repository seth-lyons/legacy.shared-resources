using System;

namespace SharedResources
{
    public class CreditInquiry
    {
        public string SequenceNumber { get; set; }
        public string Label { get; set; }
        public DateTime? InquiryDate { get; set; }
        public string Name { get; set; }
        public string CreditBusinessType { get; set; }
        public string DetailCreditBusinessType { get; set; }
        public CreditRepository[] CreditRepositories { get; set; }
    }
}
