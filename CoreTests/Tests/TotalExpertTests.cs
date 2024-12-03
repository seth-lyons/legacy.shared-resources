using SharedResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreTests.Tests
{
    public class TotalExpertTests
    {
        internal static async Task Run(ClientSettings totalExpertSettings, ClientSettings azureADSettings)
        {
            await GetTokenDetailFromKeyVaultSettings(azureADSettings);
        }

        static async Task GetTokenDetailFromClientSettings(ClientSettings totalExpertSettings, ClientSettings azureADSettings)
        {
            using (var te = new TotalExpertClient(totalExpertSettings, true, azureADSettings.ClientID, azureADSettings.ClientSecret))
            {
                var token = await te.GetTokenDetails();
            }
        }

        static async Task GetTokenDetailFromKeyVaultSettings(ClientSettings azureADSettings)
        {
            using (var te = new TotalExpertClient(azureADSettings.ClientID, azureADSettings.ClientSecret, TotalExpertEnvironment.Development))
            {
                var token = await te.GetTokenDetails();
                var users = await te.GetUsers();
            }
        }

        static async Task GetGroups(ClientSettings azureADSettings)
        {
            using (var te = new TotalExpertClient(azureADSettings.ClientID, azureADSettings.ClientSecret, TotalExpertEnvironment.Development))
            {
                var groups = await te.GetUsers();
            }
        }
    }
}
