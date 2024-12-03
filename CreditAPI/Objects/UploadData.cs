namespace SharedResources
{
    public class UploadData
    {
        public string LoanGuid { get; set; }
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
        public Person Borrower { get; set; }
        public string BorrowerPairID { get; set; }
        public int BorrowerPairNumber { get; set; }
    }
}
