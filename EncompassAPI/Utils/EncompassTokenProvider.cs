using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace SharedResources
{
    public class EncompassTokenProvider : IDisposable
    {
        bool _inFlightRetrival;

        private enum TokenOperationType
        {
            Get,
            Inspect,
            Revoke
        }

        private string _token { get; set; }
        private DateTime? _expires { get; set; }

        private readonly EncompassSettings _settings;
        public EncompassTokenProvider(EncompassSettings settings)
        {
            _settings = settings;
        }

        public async Task<string> GetToken()
        {
            if (string.IsNullOrWhiteSpace(_token) || _expires == null || DateTime.UtcNow >= _expires)
                await SetNewToken();
            return _token;
        }

        public async Task SetNewToken()
        {
            if (_inFlightRetrival)
            {
                while (_inFlightRetrival)
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                return;
            }

            try
            {
                _inFlightRetrival = true;

                if (!string.IsNullOrWhiteSpace(_token) && DateTime.UtcNow <= _expires)
                    await RevokeToken();

                var response = await GetNewToken();
                if (response.IsError)
                {
                    _token = null;
                    _expires = null;
                    return;
                }

                _token = (string)JObject.Parse(response.ResponseBody)?["access_token"];
                var inspectionResponse = await InspectToken();
                if (inspectionResponse.IsError) //Guess it will work for 29 minutes (standard lifetime has been shown to be 30 min)
                    _expires = DateTime.UtcNow.AddMinutes(29);
                else
                    _expires = Operations.UnixTimeStampToDateTime((long)JObject.Parse(inspectionResponse.ResponseBody)?["exp"]);
            }
            finally
            {
                _inFlightRetrival = false;
            }
        }

        private async Task<WebRequestResponse> GetNewToken() => await TokenOperation(TokenOperationType.Get);
        private async Task<WebRequestResponse> RevokeToken() => await TokenOperation(TokenOperationType.Revoke);
        private async Task<WebRequestResponse> InspectToken() => await TokenOperation(TokenOperationType.Inspect);

        private async Task<WebRequestResponse> TokenOperation(TokenOperationType operation)
        {
            string requestUri = $"https://api.elliemae.com/oauth2/v1/token/{(operation == TokenOperationType.Inspect ? "introspection" : operation == TokenOperationType.Revoke ? "revocation" : "")}";

            using (var client = new RestClient())
            using (var content = new FormUrlEncodedContent(operation == TokenOperationType.Get ?
                new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", $"{_settings.Username}@encompass:{_settings.InstanceID}"),
                    new KeyValuePair<string, string>("password", _settings.Password),
                }
                : new[] { new KeyValuePair<string, string>("token", _token) }))
            {
                var result = await client.Post(requestUri, content, new Dictionary<string, string> {
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                    { HttpRequestHeader.Authorization.ToString(), $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ClientID}:{_settings.ClientSecret}"))}" },
                });

                result.EnsureSuccess();
                return result;
            }
        }

        public void Dispose()
        {
            if (!string.IsNullOrWhiteSpace(_token))
                RevokeToken().Wait();
        }
    }
}
