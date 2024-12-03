using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace SharedResources
{
    public interface IGoogleOptions
    {
        KeySettings Google { get; set; }
    }

    public class GoogleClient : IDisposable
    {
        private readonly KeySettings _settings;
        private RestClient _restClient;

        public GoogleClient(KeySettings settings)
        {
            _settings = settings;
            _restClient = new RestClient();
        }

        public async Task<JObject> ValidateAddress(string address)
        {
            var response = await _restClient.Get($"https://maps.googleapis.com/maps/api/geocode/json?address={address}&key={_settings.PrivateKey}");
            return response.IsError 
                ? throw new Exception($"An error occurred contacting the Google API. {response.StatusCode}, {response.Reason}. {response.ResponseBody}") 
                : JObject.Parse(response.ResponseBody);
        }

        public void Dispose()
        {
            ((IDisposable)_restClient).Dispose();
        }
    }
}
