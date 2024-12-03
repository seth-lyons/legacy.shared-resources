using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResources
{
    public class ClientSettings
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Environment { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string TenantID { get; set; }
        public string SubscriptionId { get; set; }
        public string Resource { get; set; }
        public string Scope { get; set; }
        public string BaseAddress { get; set; }
        public string TokenAddress { get; set; }
    }

    public interface IAzureADOptions
    {
        ClientSettings AzureAD { get; set; }
    }
}
