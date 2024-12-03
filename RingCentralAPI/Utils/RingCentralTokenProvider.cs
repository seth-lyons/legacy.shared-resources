using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SharedResources
{
    public class RingCentralTokenProvider
    {
        private TokenAuthentication _authResponse { get; set; }
        private ClientSettings _settings { get; set; }
        bool _inFlightRetrival;

        public RingCentralTokenProvider(ClientSettings settings)
        {
            _settings = settings;
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
                    RequestUri = new Uri($"{_settings.BaseAddress}/oauth/token"),
                    Method = HttpMethod.Post,
                    Headers = {
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                },
                    Content = new FormUrlEncodedContent(_authResponse?.RefreshTokenExpires > DateTime.UtcNow
                        ? new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "refresh_token"),
                            new KeyValuePair<string, string>("endpoint_id", _authResponse?.EndpointId),
                            new KeyValuePair<string, string>("refresh_token", _authResponse?.RefreshToken),
                        }
                                                           : new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "password"),
                            new KeyValuePair<string, string>("username", _settings.Username),
                            new KeyValuePair<string, string>("password", _settings.Password),
                        }
                    ),
                })
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ClientID}:{_settings.ClientSecret}")));
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

        public async Task RevokeTokenAsync()
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{_settings.BaseAddress}/oauth/revoke"),
                Method = HttpMethod.Post,
                Headers = {
                        { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                    },
                Content = new FormUrlEncodedContent(
                    new[]
                    {
                            new KeyValuePair<string, string>("token", _authResponse?.Token)
                    }
                ),
            })
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ClientID}:{_settings.ClientSecret}")));
                var response = await client.SendAsync(request);
                _authResponse = null;
            }

        }
    }
}
