using Newtonsoft.Json.Linq;
using SharedResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace CoreTests.Tests
{
    public static class VelocifyTests
    {
        internal static async Task Run(CredentialSettings settings)
        {
            var client = new VelocifyClient(settings);

            var fields = await client.GetFields();
            var resp = JArray.FromObject(fields);
        }

        static async Task GetCampaignsTest(this VelocifyClient client, CredentialSettings settings)
        {
            await client.GetData();
        }
    }
}
