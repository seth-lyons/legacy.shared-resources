using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SharedResources
{
    public class SharePointhClient : IDisposable
    {
        TokenProvider _tokenProvider { get; set; }
        ClientSettings _settings { get; set; }
        RestClient _client { get; set; }

        public SharePointhClient(string clientID, string clientSecret) => Initialize(new ClientSettings { ClientID = clientID, ClientSecret = clientSecret });

        public SharePointhClient(ClientSettings settings) => Initialize(settings);

        void Initialize(ClientSettings settings)
        {
            settings.TenantID = settings.TenantID ?? "7fff43ef-4b23-4929-9880-b9d71c748c64";
            settings.Resource = settings.Resource ?? $"00000003-0000-0ff1-ce00-000000000000/nlcloans.sharepoint.com@{settings.TenantID}";
            settings.SubscriptionId = settings.SubscriptionId ?? "0c3e2a8e-de31-463f-a296-b3976e2c1d47";
            settings.BaseAddress = settings.BaseAddress ?? "https://nlcloans.sharepoint.com";
            settings.TokenAddress = settings.TokenAddress ?? $"https://accounts.accesscontrol.windows.net/{settings.TenantID}/tokens/OAuth/2";

            _settings = settings;
            _client = new RestClient(_settings.BaseAddress);
            _tokenProvider = new TokenProvider(_settings);
        }

        public async Task<JToken> GetSiteDriveInfo(string siteName)
        {
            var uri = $"/sites/nlcloans.sharepoint.com:/sites/{siteName}:/drives";
            var result = await SendRequest(uri);
            return (JArray)JObject.Parse(result.ResponseBody)?["value"];
        }

        public async Task<JArray> ListFiles(string driveId, string path)
        {
            var response = await SendRequest($"/drives/{driveId}/root:{path?.TrimEnd(' ', '\\', '/')}:/children", httpMethod: HttpMethod.Get);
            return JToken.Parse(response.ResponseBody)?["value"] as JArray;
        }

        public async Task<bool> UploadFileViaSession(byte[] file, string driveId, string path)
            => await UploadFileViaSession(file, $"/drives/{driveId}/root:{path}:/createUploadSession");

        public async Task<bool> UploadFileViaSession(byte[] file, string url)
        {
            var response = await SendRequest(url, httpMethod: HttpMethod.Put, body: @"{""item"":{""@microsoft.graph.conflictBehavior"":""rename""}}");
            var sessionInfo = JToken.Parse(response.ResponseBody);
            var uploadURL = (string)sessionInfo?["uploadUrl"];

            var contentLength = file.Length;
            var batchSize = 3000000;
            var batches = file.Batch(batchSize);
            int batchEnd = -1;

            foreach (var batch in batches)
            {
                int length = batch.Length;
                int batchStart = batchEnd + 1;
                batchEnd += length;
                using (var content = new ByteArrayContent(batch))
                {
                    content.Headers.ContentLength = length;
                    content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(batchStart, batchEnd, contentLength);
                    var uploadResponse = await SendRequest(uploadURL, httpMethod: HttpMethod.Put, body: content);
                    if (uploadResponse.IsError)
                        return false;
                }
            }

            return true;
        }

        public async Task<JArray> GetListItems(string siteName, string listName, string filter = null, string top = null, string orderBy = null, string select = null)
        {
            var uri = $"/sites/nlcloans.sharepoint.com:/sites/{siteName}:/lists/{listName}/items?" +
                $"$expand=fields{(select != null ? $"(select={select})" : "")}" +
                $"&$select=id,createdBy,lastModifiedBy{(filter != null ? $"&$filter={filter}" : "")}" +
                $"{(orderBy != null ? $"&$orderBy={orderBy}" : "")}" +
                $"{(top != null ? $"&$top={top}" : "")}";

            var result = await SendRequest(uri);
            return (JArray)JObject.Parse(result.ResponseBody)?["value"];
        }

        public async Task<JArray> GetGroupDriveItems(string groupID = "29a701ec-1196-4855-8286-837d818dd629", string path = "Direct Mail/Mail Files Automated Process")
        {
            var result = await SendRequest($"https://graph.microsoft.com/v1.0/groups/{groupID}/drive/root:/{path}:/children");
            return (JArray)JObject.Parse(result.ResponseBody)?["value"];
        }

        public async Task<byte[]> GetGroupDriveItemData(string itemId, string groupID = "29a701ec-1196-4855-8286-837d818dd629")
        {
            var result = await SendRequest($"https://graph.microsoft.com/v1.0/groups/{groupID}/drive/items/{itemId}/content", accept: "*/*", encodeReponse: true);
            return result?.ResponseBytes;
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
                throw new Exception($"Response from {requestUri}. {response.StatusCode}, {response.Reason}: {response.ResponseBody}");

            return response;
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
