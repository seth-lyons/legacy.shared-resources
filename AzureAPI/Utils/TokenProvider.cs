using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedResources
{
    public class TokenProvider
    {
        private TokenAuthentication _authResponse { get; set; }
        private ClientSettings _settings { get; set; }
        private bool _v2 { get; set; }
        bool _inFlightRetrival;

        public TokenProvider(ClientSettings settings, bool v2 = false)
        {
            _settings = settings;
            _v2 = v2;
        }

        public string Token => _authResponse.Token;

        public async Task<string> GetToken()
        {
            if (_authResponse?.Token == null || _authResponse?.Expires <= DateTime.UtcNow)
                await SetNewToken();
            return _authResponse.Token;
        }

        public async Task SetNewToken()
        {
            if (_inFlightRetrival)
            {
                while (_inFlightRetrival)
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                return;
            }

            _inFlightRetrival = true;
            try
            {
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(_settings.TokenAddress ?? $"https://login.microsoftonline.com/{(_settings.TenantID ?? "7fff43ef-4b23-4929-9880-b9d71c748c64")}/oauth2/{(_v2 ? "v2.0/" : "")}token"),
                    Method = HttpMethod.Post,
                    Headers = {
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                },
                    Content = new FormUrlEncodedContent(new[]
                        {
                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new KeyValuePair<string, string>("client_id", _settings.ClientID),
                        new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
                        (
                            _v2
                                ? new KeyValuePair<string, string>("scope", _settings.Scope ??  "https://graph.microsoft.com/.default")
                                : new KeyValuePair<string, string>("resource", _settings.Resource ??  "https://graph.microsoft.com")
                        )
                    }
                    ),
                })
                {
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    _authResponse = JsonConvert.DeserializeObject<TokenAuthentication>(await response.Content.ReadAsStringAsync());
                }
            }
            finally
            {
                _inFlightRetrival = false;
            }
        }
    }
}
