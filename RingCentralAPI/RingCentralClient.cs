using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedResources
{
    public class RingCentralClient : IDisposable
    {
        private ClientSettings _settings { get; set; }
        private RingCentralTokenProvider _tokenProvider { get; set; }
        private RestClient _client { get; set; }

        public RingCentralClient(ClientSettings settings)
        {
            _settings = settings;
            _tokenProvider = new RingCentralTokenProvider(settings);
            _client = new RestClient(_settings.BaseAddress);
        }

        public async Task<JArray> GetDirectoryEntries() => await GetAllObjects("/v1.0/account/~/directory/entries?type=User");
        public async Task<IEnumerable<T>> GetDirectoryEntries<T>() => (await GetAllObjects("/v1.0/account/~/directory/entries?type=User")).ToObject<IEnumerable<T>>();

        protected async Task<JArray> GetAllObjects(string uri, string rootObjectName = "records", int pageSize = 500)
        {
            var objects = new JArray();
            int pageNumber = 1;
            int totalPages = 0;
            do
            {
                var response = await SendRequest($"{uri}{(uri.Contains("?") ? "&" : "?")}page={pageNumber}&perPage={pageSize}");
                if (response.IsError || response.ResponseBody == null)
                    throw new Exception($"Response did not indicate success. {response.StatusCode}, {response.Reason}: {response.ResponseBody}");

                var jObj = JObject.Parse(response.ResponseBody);
                if (pageNumber == 1)
                    totalPages = (int)jObj?["paging"]?["totalPages"];
                objects.Merge((JArray)jObj?[rootObjectName]);
                pageNumber += 1;
            } while (pageNumber <= totalPages);

            return objects;
        }

        private async Task<WebRequestResponse> SendRequest(string requestUri, bool encodeReponse = false, string accept = "application/json", HttpMethod httpMethod = null, object body = null)
        {
            return await _client.Send(requestUri, body, httpMethod ?? HttpMethod.Get,
                new Dictionary<string, string> { { "Accept", accept }, { "Authorization", $"Bearer {await _tokenProvider.GetToken()}" } },
                encodeReponse);
        }

        public void Dispose()
        {
            _tokenProvider.RevokeTokenAsync().GetAwaiter().GetResult();
        }
    }
}
