using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedResources
{
    public class AzureRestClient : IDisposable
    {
        TokenProvider _tokenProvider { get; set; }
        ClientSettings _settings { get; set; }
        RestClient _client { get; set; }
        string _apiVersion { get; set; }

        public AzureRestClient(string clientID, string clientSecret, string apiVersion = "2020-01-01")
        {
            _settings = new ClientSettings
            {
                ClientID = clientID,
                ClientSecret = clientSecret,
                Resource = "https://management.azure.com/",
                SubscriptionId = "0c3e2a8e-de31-463f-a296-b3976e2c1d47",
                TenantID = "7fff43ef-4b23-4929-9880-b9d71c748c64",
                BaseAddress = $"https://management.azure.com/subscriptions/0c3e2a8e-de31-463f-a296-b3976e2c1d47"
            };
            _apiVersion = apiVersion;
            _client = new RestClient(_settings.BaseAddress);
            _tokenProvider = new TokenProvider(_settings);
        }

        public AzureRestClient(ClientSettings settings)
        {
            _settings = new ClientSettings
            {
                ClientID = settings.ClientID,
                ClientSecret = settings.ClientSecret,
                Resource = "https://management.azure.com/",
                SubscriptionId = "0c3e2a8e-de31-463f-a296-b3976e2c1d47",
                TenantID = "7fff43ef-4b23-4929-9880-b9d71c748c64",
                BaseAddress = $"https://management.azure.com/subscriptions/0c3e2a8e-de31-463f-a296-b3976e2c1d47",
            };
            _apiVersion = "2020-01-01";
            _client = new RestClient(_settings.BaseAddress);
            _tokenProvider = new TokenProvider(_settings);
        }

        public async Task<JArray> GetSites(string resourceGroup)
            => (JArray)JObject.Parse((await SendRequest($"/resourceGroups/{resourceGroup}/providers/Microsoft.Web/sites?api-version={_apiVersion}")).ResponseBody)["value"];

        public async Task<JArray> GetOpenSecurityInsightCases(string severity = null)
            => await GetAllItems($"/resourceGroups/azutility/providers/Microsoft.OperationalInsights/workspaces/NLCOMS/providers/Microsoft.SecurityInsights/incidents?api-version={_apiVersion}&$filter={(severity.IsEmpty() ? "" : $"properties/severity eq '{severity}' and ")}properties/status ne 'Closed'&$orderby=properties/createdTimeUtc asc&$top=50000");

        public async Task<JArray> GetSecurityInsightCases()
            => await GetAllItems($"/resourceGroups/azutility/providers/Microsoft.OperationalInsights/workspaces/NLCOMS/providers/Microsoft.SecurityInsights/incidents?api-version={_apiVersion}&$orderby=properties/createdTimeUtc asc&$top=10000");

        public async Task<JObject> UpdateSecurityInsightCase(string incidentId, object body)
        {
            return JObject.Parse((await SendRequest($"/resourceGroups/azutility/providers/Microsoft.OperationalInsights/workspaces/NLCOMS/providers/Microsoft.SecurityInsights/incidents/{incidentId}?api-version={_apiVersion}", body, httpMethod: HttpMethod.Put)).ResponseBody);
        }

        public async Task<JObject> GetSecurityInsightCase(string incidentId)
        {
            return JObject.Parse((await SendRequest($"/resourceGroups/azutility/providers/Microsoft.OperationalInsights/workspaces/NLCOMS/providers/Microsoft.SecurityInsights/incidents/{incidentId}?api-version={_apiVersion}")).ResponseBody);
        }

        public async Task<JObject> GetSecurityInsightCase(int incidentNumber)
        {
            return JObject.Parse((await SendRequest($"/resourceGroups/azutility/providers/Microsoft.OperationalInsights/workspaces/NLCOMS/providers/Microsoft.SecurityInsights/incidents?api-version={_apiVersion}&$filter=properties/incidentNumber eq {incidentNumber}")).ResponseBody)?["value"]?.FirstOrDefault() as JObject;
        }

        private async Task<JArray> GetAllItems(string uri, string listName = "value", string next = "nextLink")
        {
            var items = new JArray();
            do
            {
                var json = JObject.Parse((await SendRequest(uri)).ResponseBody);
                items.Merge((JArray)json?[listName]);
                uri = (string)json?[next];
            } while (!uri.IsEmpty());
            return items;
        }

        private async Task<WebRequestResponse> SendRequest(string requestUri, object body = null, bool encodeReponse = false, string accept = "application/json", HttpMethod httpMethod = null)
        {
            var response = await _client.Send(
                requestUri,
                body,
                httpMethod ?? (body == null ? HttpMethod.Get : HttpMethod.Post),
                new Dictionary<string, string> {
                    { "Accept", accept },
                    { "Authorization", $"Bearer {await _tokenProvider.GetToken()}" } },
                encodeReponse);

            if (response.IsError)
            {
                var exception = new Exception($"An exception occurred while completing the request. {response.StatusCode}, {response.Reason}: - {response.ResponseBody}");
                exception.Data.Add("StatusCode", response.StatusCode);
                exception.Data.Add("Reason", response.Reason);
                if (response.ResponseHeaders.ContainsKey("Retry-After"))
                    exception.Data.Add("Retry-After", response.ResponseHeaders["Retry-After"]?.FirstOrDefault());
                throw exception;
            }
            return response;
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
