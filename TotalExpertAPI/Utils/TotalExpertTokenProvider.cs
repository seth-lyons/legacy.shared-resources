using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SharedResources
{

    public class TotalExpertTokenProvider
    {
        private TokenAuthentication _authResponse { get; set; }
        private KeyVaultBackup _keyVaultBackup { get; set; }
        internal ClientSettings Settings { get; set; }

        bool _useTokenBackup;
        bool _inFlightRetrival;
        public TotalExpertTokenProvider(ClientSettings settings, bool useTokenBackup = true, string azureClientID = null, string azureClientSecret = null)
        {
            Settings = settings;
            _useTokenBackup = useTokenBackup;

            var needCreds = settings.ClientID.IsEmpty() || settings.ClientSecret.IsEmpty();
            if (_useTokenBackup || needCreds)
            {
                if (azureClientID.IsEmpty() || azureClientSecret.IsEmpty())
                {
                    throw new Exception(_useTokenBackup
                        ? "To use token backup, Azure client credentials are required to access the Key Vault."
                        : "Insufficient credentials provided. To use gather TE credentials from the Key Vault, Azure client credentials are required.");
                }

                _keyVaultBackup = new KeyVaultBackup(azureClientID, azureClientSecret, isDev: (settings.Environment.Is("Dev") || settings.Environment.Is("Development")));
                (needCreds ? SetAllFromBackup() : SetTokenFromBackup()).GetAwaiter().GetResult();
            }
        }

        public async Task<TokenAuthentication> GetTokenDetails()
        {
            if (_authResponse?.Token == null || _authResponse?.Expired != false)
                await SetNewToken();
            return _authResponse;
        }

        public async Task<string> GetToken()
        {
            if (_authResponse?.Token == null || _authResponse?.Expires <= DateTime.UtcNow)
                await SetNewToken();
            return _authResponse.Token;
        }

        private async Task SetTokenFromBackup()
        {
            try
            {
                var details = await _keyVaultBackup.GetTokenFromBackup();
                if (!string.IsNullOrWhiteSpace(details?.Token))
                    SetTokenDetails(details);
            }
            catch { }
        }

        private async Task SetAllFromBackup()
        {
            try
            {
                var values = await _keyVaultBackup.GetAllFromBackup();
                if (!string.IsNullOrWhiteSpace(values.TokenDetails?.Token))
                    SetTokenDetails(values.TokenDetails);
                if (!string.IsNullOrWhiteSpace(values.ClientID))
                    Settings.ClientID = values.ClientID;
                if (!string.IsNullOrWhiteSpace(values.ClientSecret))
                    Settings.ClientSecret = values.ClientSecret;
            }
            catch (Exception e)
            {
            }
        }

        private async Task SetBackupToken()
        {
            try
            {
                await _keyVaultBackup.SetTokenBackup(_authResponse);
            }
            catch { }
        }

        public void SetTokenDetails(TokenAuthentication tokenDetails) => _authResponse = tokenDetails;

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
                    RequestUri = new Uri($"{Settings.BaseAddress}/token"),
                    Method = HttpMethod.Post,
                    Headers = {
                        { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
                        { HttpRequestHeader.Authorization.ToString(), $"Basic {(Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Settings.ClientID}:{Settings.ClientSecret}")))}" },
                    },
                    Content = new FormUrlEncodedContent(new[] {
                            new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        }
                    ),
                })
                {
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    _authResponse = JsonConvert.DeserializeObject<TokenAuthentication>(await response.Content.ReadAsStringAsync());

                    if (_useTokenBackup) await SetBackupToken();
                }
            }
            finally
            {
                _inFlightRetrival = false;
            }
        }
    }
}
