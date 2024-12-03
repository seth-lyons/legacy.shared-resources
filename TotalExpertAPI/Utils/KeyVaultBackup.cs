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
    //BUIDING THIS SEPARATE SO I DONT HAVE TO INCLUDE A REFERENCE TO Azure.APIs
    internal class KeyVaultBackup
    {
        private TokenAuthentication _authResponse { get; set; }
        private string _clientID { get; set; }
        private string _clientSecret { get; set; }
        private string _apiVersion { get; set; }
        private bool _isDev { get; set; }

        bool _inFlightRetrival;

        //Usually, I would default to DEV. I'm defaulting to prod here since I will most often be using the production ENV with this lib
        public KeyVaultBackup(string clientID, string clientSecret, string apiVersion = "7.1", bool isDev = false)
        {
            _clientID = clientID;
            _clientSecret = clientSecret;
            _apiVersion = apiVersion;
            _isDev = isDev;
        }

        private async Task<string> GetToken()
        {
            if (_authResponse?.Token == null || _authResponse?.Expires <= DateTime.UtcNow)
                await SetNewToken();
            return _authResponse.Token;
        }

        private async Task SetNewToken()
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
                    RequestUri = new Uri("https://login.microsoftonline.com/7fff43ef-4b23-4929-9880-b9d71c748c64/oauth2/token"),
                    Method = HttpMethod.Post,
                    Headers = {
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                },
                    Content = new FormUrlEncodedContent(new[]
                        {
                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new KeyValuePair<string, string>("client_id", _clientID),
                        new KeyValuePair<string, string>("client_secret", _clientSecret),
                        new KeyValuePair<string, string>("resource", "https://vault.azure.net")
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

        internal async Task<(TokenAuthentication TokenDetails, string ClientID, string ClientSecret)> GetAllFromBackup()
        {
            using (var client = new HttpClient())
            using (var tokenRequest = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://totalexpert.vault.azure.net/secrets/tokenbackup{(_isDev ? "-dev" : "")}?api-version={_apiVersion}"),
                Method = HttpMethod.Get,
            })
            using (var clientIDRequest = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://totalexpert.vault.azure.net/secrets/clientid{(_isDev ? "-dev" : "")}?api-version={_apiVersion}"),
                Method = HttpMethod.Get,
            })
            using (var clientSecretRequest = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://totalexpert.vault.azure.net/secrets/clientsecret{(_isDev ? "-dev" : "")}?api-version={_apiVersion}"),
                Method = HttpMethod.Get,
            })
            {
                var authHeader = new AuthenticationHeaderValue("Bearer", await GetToken());
                tokenRequest.Headers.Authorization = authHeader;
                clientIDRequest.Headers.Authorization = authHeader;
                clientSecretRequest.Headers.Authorization = authHeader;

                var response = await client.SendAsync(tokenRequest);
                response.EnsureSuccessStatusCode();
                var value = (string)Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync())?["value"];
                var tokenDetails = JsonConvert.DeserializeObject<TokenAuthentication>(Encoding.UTF8.GetString(Convert.FromBase64String(value)));

                response = await client.SendAsync(clientIDRequest);
                response.EnsureSuccessStatusCode();
                var clientID = (string)Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync())?["value"];

                response = await client.SendAsync(clientSecretRequest);
                response.EnsureSuccessStatusCode();
                var clientSecret = (string)Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync())?["value"];

                return (tokenDetails, clientID, clientSecret);
            }
        }

        internal async Task<TokenAuthentication> GetTokenFromBackup()
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://totalexpert.vault.azure.net/secrets/tokenbackup{(_isDev ? "-dev" : "")}?api-version={_apiVersion}"),
                Method = HttpMethod.Get,
            })
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetToken());
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var value = (string)Newtonsoft.Json.Linq.JObject.Parse(await response.Content.ReadAsStringAsync())?["value"];
                return JsonConvert.DeserializeObject<TokenAuthentication>(Encoding.UTF8.GetString(Convert.FromBase64String(value)));
            }
        }

        internal async Task SetTokenBackup(TokenAuthentication tokenAuthentication)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://totalexpert.vault.azure.net/secrets/tokenbackup{(_isDev ? "-dev" : "")}?api-version={_apiVersion}"),
                Method = HttpMethod.Put,
            })
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetToken());
                request.Content = new StringContent($"{{\"value\": \"{Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tokenAuthentication)))}\"}}");
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
