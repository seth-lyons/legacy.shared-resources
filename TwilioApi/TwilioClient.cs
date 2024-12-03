using Newtonsoft.Json.Linq;
using SharedResources;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SharedResources
{
    public class TwilioClient : IDisposable
    {
        TwilioSettings _settings { get; set; }
        private RestClient _client;

        public TwilioClient(TwilioSettings settings)
        {
            _client = new RestClient("https://api.twilio.com/2010-04-01");
            _settings = settings;
        }

        public async Task<JObject> SendMessage(string to, string message, string mediaUrl = null, string statusCallback = null)
        {
            var body = new Dictionary<string, string>
            {
                { "From", _settings.From },
                { "To", to },
                { "Body", message }
            };

            if (!_settings.MessagingServiceSid.IsEmpty()) body.Add("MessagingServiceSid", _settings.MessagingServiceSid);
            if (mediaUrl != null) body.Add("MediaUrl", mediaUrl);
            if (statusCallback != null) body.Add("StatusCallback", statusCallback);

            var response = await SendRequest($"/Accounts/{_settings.AccountSid}/Messages.json", new FormUrlEncodedContent(body));
            response.EnsureSuccess();
            return JObject.Parse(response.ResponseBody);
        }

        private async Task<WebRequestResponse> SendRequest(string requestUri, object body = null, HttpMethod httpMethod = null, bool encodeResponse = false)
        {
            return await _client.Send(requestUri, body, httpMethod,
                new Dictionary<string, string> {
                    { HttpRequestHeader.Authorization.ToString(), $"Basic {(Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}")))}" }
               }, encodeResponse
            );
        }
        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
