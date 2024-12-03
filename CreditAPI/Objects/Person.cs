namespace SharedResources
{
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string SSN { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Role { get; set; }
        public string NMLS { get; set; }
        public Location PresentAddress { get; set; }
        public Location MailingAddress { get; set; }
    }
}
