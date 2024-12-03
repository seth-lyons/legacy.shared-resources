using Newtonsoft.Json.Linq;
using SharedResources;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedResources
{
    public class GoHighLevelClient
    {
        private RestClient _client;
        private readonly string _apiKey;
        public GoHighLevelClient(string apiKey)
        {
            _apiKey = apiKey;
            _client = new RestClient("https://rest.gohighlevel.com/v1");
        }

        public async Task<JObject> GetContact(string id) => (await GetResponse($"/contacts/{id}"))?["contact"] as JObject;

        /// <param name="query">It will search on these fields: Name, Phone, Email, Tags, and Company Name</param>
        public async Task<JArray> GetContacts(string query = null) => await GetAllObjects($"/contacts{(query.IsEmpty() ? "" : $"?query={query}")}", "contacts");

        protected async Task<JArray> GetAllObjects(string uri, string rootObjectName, int pageSize = 100)
        {
            var objects = new JArray();
            var address = $"{uri}{(uri.Contains("?") ? "&" : "?")}limit={pageSize}";
            do
            {
                var response = await SendRequest(address);
                if (response.IsError || response.ResponseBody == null)
                    throw new Exception($"Response did not indicate success. {response.StatusCode}, {response.Reason}: {response.ResponseBody}");

                var jObj = JObject.Parse(response.ResponseBody);
                address = (string)jObj?["meta"]?["nextPageUrl"];
                objects.Merge((JArray)jObj?[rootObjectName]);
            } while (!address.IsEmpty());

            return objects;
        }

        protected async Task<JToken> GetResponse(string uri, object body = null, HttpMethod method = null)
        {
            var response = await SendRequest(uri, httpMethod: method ?? (body == null ? HttpMethod.Get : HttpMethod.Post), body: body);
            if (response.IsError)
                throw new Exception($"Response did not indicate success. {response.StatusCode}, {response.Reason}: {response.ResponseBody}");
            return string.IsNullOrWhiteSpace(response.ResponseBody) ? null : JToken.Parse(response.ResponseBody);
        }


        private async Task<WebRequestResponse> SendRequest(string requestUri, bool encodeReponse = false, string accept = "application/json", HttpMethod httpMethod = null, object body = null)
        {
            return await _client.Send(requestUri, body, httpMethod ?? HttpMethod.Get, new Dictionary<string, string> { { "Accept", accept }, { "Authorization", $"Bearer {_apiKey}" } }, encodeReponse);
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
