using System.Collections.Generic;

namespace SharedResources
{
    public class Party
    {
        public string SequenceNumber { get; set; }
        public string Label { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public List<Role> Roles { get; set; }
    }
}
