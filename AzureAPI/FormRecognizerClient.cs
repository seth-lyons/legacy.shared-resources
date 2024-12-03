using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SharedResources
{
    public class FormRecognizerClient
    {
        //TODO: Copied from old project. Needs updated
        // readonly string _baseAddress = "https://encompassformrecognizer.cognitiveservices.azure.com/formrecognizer/v2.0/";

        readonly string _apiKey;
        readonly string _baseAddress;
        readonly string _version;

        public FormRecognizerClient(string endpoint, string apiKey, string version = "2.0")
        {
            _apiKey = apiKey;
            _version = $"v{version}";
            _baseAddress = endpoint.TrimEnd('/') + $"/formrecognizer/{_version}";
        }

        public async Task<JToken> GetResults(string url) =>
            await ExecuteRequest(url).ConfigureAwait(false);

        public async Task<JToken> AnalyzeLayout(byte[] file) =>
            await ExecuteRequest($"{_baseAddress}/layout/analyze", HttpMethod.Post, file, "application/octet-stream").ConfigureAwait(false);

        public async Task<JToken> AnalyzeReceipt(byte[] file, bool includeTextDetails = false) =>
            await ExecuteRequest($"{_baseAddress}/prebuilt/receipt/analyze?includeTextDetails={includeTextDetails}", HttpMethod.Post, file, "application/octet-stream").ConfigureAwait(false);

        public async Task<JToken> AnalyzeForm(byte[] file, string modelId, bool includeTextDetails = false) =>
           await ExecuteRequest($"{_baseAddress}/custom/models/{modelId}/analyze?includeTextDetails={includeTextDetails}", HttpMethod.Post, file, "application/pdf").ConfigureAwait(false);

        public async Task<JToken> TrainModel(string sasUrl, string folder = null)
        {
            return await ExecuteRequest($"{_baseAddress}/custom/models", HttpMethod.Post, new
            {
                source = sasUrl,
                sourceFilter = new
                {
                    prefix = folder,
                    includeSubFolders = true
                },
                useLabelFile = false
            }).ConfigureAwait(false);
        }

        public async Task<JToken> GetModels(bool summary = true) =>
            await ExecuteRequest($"{_baseAddress}/custom/models?op={summary}").ConfigureAwait(false);

        public async Task<JToken> GetModel(string modelId, bool includeKeys = true) =>
            await ExecuteRequest($"{_baseAddress}/custom/models/{modelId}?includeKeys={includeKeys}").ConfigureAwait(false);

        public async Task<JToken> GetKeys(string modelId) =>
            await ExecuteRequest($"{_baseAddress}/custom/models/{modelId}/keys").ConfigureAwait(false);

        public async Task<JToken> DeleteModel(string modelId) =>
            await ExecuteRequest($"{_baseAddress}/custom/models/{modelId}", HttpMethod.Delete).ConfigureAwait(false);

        public async Task<JToken> GenerateCopyAuthorization(string resourceEndpoint = null) =>
            await ExecuteRequest($"{(resourceEndpoint == null ? _baseAddress : resourceEndpoint.TrimEnd('/') + $"/formrecognizer/{_version}")}/custom/models/copyAuthorization", HttpMethod.Post).ConfigureAwait(false);

        public async Task<JToken> CopyModel(
            string sourceModelId,
            string targetResourceId,
            string targetResourceRegion,
            string targetModelId,
            string accessToken,
            long expirationDateTimeTicks,
            string originResourceEndpoint = null)
        {
            return await ExecuteRequest($"{(originResourceEndpoint == null ? _baseAddress : originResourceEndpoint.TrimEnd('/') + $"/formrecognizer/{_version}")}/custom/models/{sourceModelId}/copy",
                HttpMethod.Post,
                new
                {
                    targetResourceId,
                    targetResourceRegion,
                    copyAuthorization = new
                    {
                        modelId = targetModelId,
                        accessToken,
                        expirationDateTimeTicks
                    }
                }).ConfigureAwait(false);
        }

        async Task<JToken> ExecuteRequest(string requestUri, HttpMethod method = null, object body = null, string contentType = "application/json")
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(method ?? HttpMethod.Get, requestUri))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey ?? throw new ArgumentNullException("Secret"));
                if (body != null)
                {
                    if (body.GetType() == typeof(byte[]))
                        request.Content = new ByteArrayContent((byte[])body);
                    else
                        request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.ASCII);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                }

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return string.IsNullOrWhiteSpace(content) ?
                        JObject.FromObject(new
                        {
                            status = response.StatusCode,
                            operationLocation = response.Headers.Contains("Operation-Location") ? response.Headers?.GetValues("Operation-Location")?.FirstOrDefault() 
                                : response.Headers.Contains("Location") ? response.Headers?.GetValues("Location")?.FirstOrDefault() 
                                : null
                        }) :
                        JToken.Parse(content);
                }
            }
        }

        static string GetMIME(string filePath, bool isReceipt = false)
        {
            var ext = Path.GetExtension(filePath)?.ToUpper();
            return
                ext == ".PDF" ? "application/pdf" :
                ext == ".PNG" ? "image/png" :
                ext == ".JPG" || ext == ".JPEG" ? "image/jpeg" :
                ext == ".BMP" && isReceipt ? "image/bmp" :
                (ext == ".TIF" || ext == ".TIFF") && isReceipt ? "image/tiff" :
                throw new FormatException($"Unsupported MIME type. Supported file types: PDF, PNG, JPEG, JPG{(isReceipt ? ", TIF, TIFF, BMP" : "")}");
        }
    }
}
