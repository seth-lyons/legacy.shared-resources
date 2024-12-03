using Newtonsoft.Json;
using SharedResources.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharedResources
{
    public class RestClient : IDisposable, IRestClient
    {
        private HttpClient _client { get; set; }
        readonly string _baseAddress;

        readonly bool _disableRetry;
        private int _timeout;
        public RestClient(string baseAddress = null, int timeout = 300, bool disableRetry = false)
        {
            _baseAddress = baseAddress;
            _timeout = timeout;
            _disableRetry = disableRetry;
            InitializeClient();
        }

        public void InitializeClient()
        {
            if (_client != null)
                try { _client.Dispose(); } catch { }

            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(_timeout);
        }
        public async Task<WebRequestResponse> Post(string uri, object body, string token, bool encodeReponse = false) =>
            await SendRequest(uri, body, HttpMethod.Post, headers: new[] { new KeyValuePair<string, string>("Authorization", $"Bearer {token}") }, encodeReponse: encodeReponse).ConfigureAwait(false);

        public async Task<WebRequestResponse> Post(string uri, object body, IEnumerable<KeyValuePair<string, string>> headers = null, bool encodeReponse = false) =>
            await SendRequest(uri, body, HttpMethod.Post, headers: headers, encodeReponse: encodeReponse).ConfigureAwait(false);

        public async Task<WebRequestResponse> Get(string uri, string token, bool encodeReponse = false) =>
            await SendRequest(uri, headers: new[] { new KeyValuePair<string, string>("Authorization", $"Bearer {token}") }, encodeReponse: encodeReponse).ConfigureAwait(false);

        public async Task<WebRequestResponse> Get(string uri, IEnumerable<KeyValuePair<string, string>> headers = null, bool encodeReponse = false) =>
            await SendRequest(uri, headers: headers, encodeReponse: encodeReponse).ConfigureAwait(false);

        public async Task<WebRequestResponse> Send(string uri, object body = null, HttpMethod method = null, IEnumerable<KeyValuePair<string, string>> headers = null, bool encodeReponse = false) =>
            await SendRequest(uri, body, method, headers, encodeReponse).ConfigureAwait(false);

        async Task<WebRequestResponse> SendRequest(string uri, object body = null, HttpMethod method = null, IEnumerable<KeyValuePair<string, string>> headers = null, bool encodeReponse = false, bool isRetry = false)
        {
            try
            {
                if (!_baseAddress.IsEmpty() && !(new Regex("^(http(s)?).*$", RegexOptions.IgnoreCase).IsMatch(uri)))
                    uri = $"{_baseAddress.TrimEnd('/')}/{uri.TrimStart('/')}";

                using (var request = new HttpRequestMessage(method ?? (body == null ? HttpMethod.Get : HttpMethod.Post), uri))
                {
                    if (headers?.Any() == true)
                    {
                        headers.ForEach(header =>
                        {
                            if (header.Key.Is("Accept"))
                                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(header.Value));
                            else if (header.Key.Is("Authorization") || header.Key.Is("Authentication"))//Authorization
                            {
                                var split = header.Value.Split(new[] { ' ' }, 2);
                                request.Headers.Authorization = new AuthenticationHeaderValue(split[0], split[1]);
                            }
                            else
                                request.Headers.Add(header.Key, header.Value);
                        });
                    }

                    if (body != null)
                    {
                        if (body is HttpContent)
                            request.Content = (HttpContent)body;
                        else
                        {
                            request.Content = new StringContent(body is string ? (string)body : JsonConvert.SerializeObject(body, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None }));
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                        }
                    }

                    using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (request?.Content != null)
                            request.Content.Dispose(); // Just making sure

                        var isError = response?.IsSuccessStatusCode != true;
                        var bytes = !isError && encodeReponse && response?.Content != null ? await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false) : null;
                        return new WebRequestResponse
                        {
                            RequestUri = uri,
                            ResponseBody = isError || !encodeReponse ? response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false) : null, //Convert.ToBase64String(bytes),
                            ResponseHeaders = response?.Headers?.ToDictionary(a => a.Key, a => a.Value),
                            ResponseBytes = bytes,
                            IsError = isError,
                            StatusCode = response.StatusCode,
                            Reason = response.ReasonPhrase,
                            IsEncoded = isError ? false : encodeReponse
                        };
                    }
                }
            }
            catch (Exception e)
            {
                //InitializeClient(); // Get a new client any time there is an exception
                if (isRetry || _disableRetry || body is HttpContent)
                    return new WebRequestResponse
                    {
                        IsEncoded = false,
                        IsError = true,
                        Reason = e.Message,
                        RequestUri = uri,
                        StatusCode = HttpStatusCode.InternalServerError,
                        ResponseBody = e.ToString()
                    };
                else
                    return await SendRequest(uri, body, method, headers, encodeReponse, true).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
