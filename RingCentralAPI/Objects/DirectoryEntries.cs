using System.Collections.Generic;

namespace SharedResources.RingCentral
{
    public class Record
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public string ExtensionNumber { get; set; }
        public Account Account { get; set; }
        public List<Phonenumber> PhoneNumbers { get; set; }
        public Site Site { get; set; }
    }

    public class Account
    {
        public string Id { get; set; }
    }

    public class Site
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Phonenumber
    {
        public string PhoneNumber { get; set; }
        public string Type { get; set; }
        public string UsageType { get; set; }
    }
}
