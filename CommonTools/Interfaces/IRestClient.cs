using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedResources
{
    public interface IRestClient
    {
        Task<WebRequestResponse> Post(string uri, object body, string token, bool encodeReponse = false);
        Task<WebRequestResponse> Post(string uri, object body, IEnumerable<KeyValuePair<string, string>> headers = null, bool encodeReponse = false);
        Task<WebRequestResponse> Get(string uri, string token, bool encodeReponse = false);
        Task<WebRequestResponse> Get(string uri, IEnumerable<KeyValuePair<string, string>> headers = null, bool encodeReponse = false);
        Task<WebRequestResponse> Send(string uri, object body = null, HttpMethod method = null, IEnumerable<KeyValuePair<string, string>> headers = null, bool encodeReponse = false);
    }
}
