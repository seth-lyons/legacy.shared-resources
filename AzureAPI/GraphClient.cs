using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedResources
{
    public enum DriveType
    {
        User,
        Group,
        Site,
        Drive
    };
    public class GraphClient : IDisposable
    {
        TokenProvider _tokenProvider { get; set; }
        ClientSettings _settings { get; set; }
        RestClient _client { get; set; }

        public async Task RefreshToken() => await _tokenProvider?.GetToken();

        public GraphClient(string clientID, string clientSecret) => Initialize(new ClientSettings { ClientID = clientID, ClientSecret = clientSecret });

        public GraphClient(ClientSettings settings) => Initialize(settings);

        void Initialize(ClientSettings settings)
        {
            settings.Resource = settings.Resource ?? "https://graph.microsoft.com";
            settings.SubscriptionId = settings.SubscriptionId ?? "0c3e2a8e-de31-463f-a296-b3976e2c1d47";
            settings.TenantID = settings.TenantID ?? "7fff43ef-4b23-4929-9880-b9d71c748c64";
            settings.BaseAddress = settings.BaseAddress ?? "https://graph.microsoft.com/v1.0";

            _settings = settings;
            _client = new RestClient(_settings.BaseAddress);
            _tokenProvider = new TokenProvider(_settings, true);
        }

        public async Task<JArray> ListFiles(string driveId, string path)
            => await GetAllItems($"/drives/{driveId}/root:{path?.TrimEnd(' ', '\\', '/')}:/children");

        public async Task<JToken> UploadFile(byte[] file, string driveId, string path)
            => await UploadFile(file, $"/drives/{driveId}/root:{path}:/content");

        public async Task<JToken> UploadFile(byte[] file, string url)
        {
            if (file.Length > 3000000) //upload large file via session
            {
                var success = await UploadFileViaSession(file, url.Replace(":/content", ":/createUploadSession"));
                return new JObject
                {
                    ["Status"] = "Uploaded via session",
                    ["Success"] = success,
                };
            }
            else
            {
                using (var content = new ByteArrayContent(file))
                {
                    var response = await SendRequest(url, body: content, httpMethod: HttpMethod.Put);
                    return JToken.Parse(response.ResponseBody);
                }
            }
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
                    _ = await SendRequest(uploadURL, httpMethod: HttpMethod.Put, body: content);
                }
            }

            return true;
        }

        //use list ID if name contains a /
        public async Task<JArray> GetListItems(string siteName, string listName, string filter = null, string top = null, string orderBy = null, string select = null)
            => await GetAllItems($"/sites/nlcloans.sharepoint.com:/sites/{siteName}:/lists/{listName}/items?" +
                $"$expand=fields{(select != null ? $"(select={select})" : "")}" +
                $"&$select=id,createdBy,lastModifiedBy{(filter != null ? $"&$filter={filter}" : "")}" +
                $"{(orderBy != null ? $"&$orderBy={orderBy}" : "")}" +
                $"{(top != null ? $"&$top={top}" : "")}");


        /// <summary>
        /// Lists drives
        /// </summary>
        /// <param name="identifier">Site name, user name, or group ID</param>
        /// <param name="driveType">The drive type to be retrieved</param>
        /// <returns></returns>
        public async Task<JToken> GetDrives(string identifier, DriveType driveType)
            => await GetAllItems(
                  driveType == DriveType.Site ? $"/sites/nlcloans.sharepoint.com:/sites/{identifier}:/drives"
                : driveType == DriveType.Group ? $"/groups/{identifier}/drives"
                : driveType == DriveType.User ? $"/users/{identifier}/drives"
                : $"/drives/{identifier}"
            );


        public async Task<JToken> GetDriveItems(string identifier, DriveType driveType, string path = "")
            => await GetAllItems(
                  driveType == DriveType.Site ? $"/sites/nlcloans.sharepoint.com:/sites/{identifier}:/drive/root:/{path}:/children" //TODO: Doesnt Work
                : driveType == DriveType.Group ? $"groups/{identifier}/drive/{(path.IsEmpty() ? "root" : $"root:/{path?.Trim('/')}:")}/children"
                : driveType == DriveType.User ? $"users/{identifier}/drive/{(path.IsEmpty() ? "root" : $"root:/{path?.Trim('/')}:")}/children"
                : $"drives/{identifier}/{(path.IsEmpty() ? "root" : $"root:/{path?.Trim('/')}:")}/children"
            );

        public async Task<byte[]> GetDriveItemData(string locationIdentifier, string itemId, DriveType driveType)
        {
            var result = await SendRequest(
                 (driveType == DriveType.Site ? $"/sites/nlcloans.sharepoint.com:/sites/{locationIdentifier}:/drive/items/{itemId}/content" //TODO: Doesnt Work
                : driveType == DriveType.Group ? $"groups/{locationIdentifier}/drive/items/{itemId}/content"
                : driveType == DriveType.User ? $"users/{locationIdentifier}/drive/items/{itemId}/content"
                : $"drives/{locationIdentifier}/items/{itemId}/content"),

                $"/groups/{locationIdentifier}/drive",
                accept: "*/*",
                encodeReponse: true);
            return result?.ResponseBytes;
        }

        public async Task<JArray> GetAllUsers()
            => await GetAllItems($"/users?$top=999");

        public async Task<JArray> GetSharePointUsers(string siteName = null, string select = null)
            => await GetAllItems((siteName.IsEmpty() ? "/sites/root/lists/User Information List/items" : $"/sites/nlcloans.sharepoint.com:/sites/{siteName}:/lists/User Information List/items") + $"?$expand=fields{(select.IsEmpty() ? "" : $"($select={select})")}");

        public async Task<JObject> GetSharePointUser(string userId, string siteName = null, string select = null)
            => await GetJsonResponse((siteName.IsEmpty() ? "/sites/root/lists/User Information List/items" : $"/sites/nlcloans.sharepoint.com:/sites/{siteName}:/lists/User Information List/items") + $"/{userId}?$expand=fields{(select.IsEmpty() ? "" : $"($select={select})")}");

        public async Task<JObject> GetUser(string email, string select = null)
            => await GetJsonResponse($"/users/{email}{(select.IsEmpty() ? "" : $"?$select={select}")}");

        public async Task<bool> AddUserToGroup(string groupId, string username)
        {
            var uri = $"/groups/{groupId}/members/$ref";
            return !(await SendRequest(uri, httpMethod: HttpMethod.Post, body: new JObject { ["@odata.id"] = $"https://graph.microsoft.com/v1.0/users/{username}" })).IsError;
        }

        public async Task<JObject> AddOrUpdateListItem(string siteName, string listName, JObject item, string id = null)
        {
            var uri = $"/sites/nlcloans.sharepoint.com:/sites/{siteName}:/lists/{listName}/items/{(id == null ? "" : id)}";
            var result = await SendRequest(uri, httpMethod: id == null ? HttpMethod.Post : new HttpMethod("PATCH"), body: item);
            return JObject.Parse(result.ResponseBody);
        }

        public async Task<bool> DeleteListItem(string siteName, string listName, string id)
        {
            var uri = $"/sites/nlcloans.sharepoint.com:/sites/{siteName}:/lists/{listName}/items/{(id == null ? "" : id)}";
            return !(await SendRequest(uri, httpMethod: HttpMethod.Delete)).IsError;
        }

        private async Task<JObject> GetJsonResponse(string uri) => JObject.Parse((await SendRequest(uri)).ResponseBody);

        private async Task<T> GetJsonResponse<T>(string uri) where T : JToken
            => (T)JToken.Parse((await SendRequest(uri)).ResponseBody);

        private async Task<JArray> GetAllItems(string uri, string listName = "value", string next = "@odata.nextLink")
        {
            var items = new JArray();
            do
            {
                var json = JObject.Parse((await SendRequest(uri)).ResponseBody);
                ((JArray)json?[listName])?.ForEach(user => items.Add(user));
                uri = (string)json?[next];
            } while (!uri.IsEmpty());
            return items;
        }

        public async Task<WebRequestResponse> SendRequest(string requestUri, object body = null, bool encodeReponse = false, string accept = "application/json", HttpMethod httpMethod = null)
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
