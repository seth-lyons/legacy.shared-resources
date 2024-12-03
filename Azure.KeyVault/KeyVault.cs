using Microsoft.Azure.KeyVault;
using System;
using System.Security.Cryptography.X509Certificates;

namespace SharedResources
{
    public class KeyVault : IDisposable
    {
        private KeyVaultClient _client { get; set; }
        private AuthenticationClient _authClient { get; set; }
        private readonly string _vaultUrl;

        public string GetSecretValue(string secret, string vaultUrl = null) => _client.GetSecretAsync(vaultUrl ?? _vaultUrl, secret).Result?.Value;

        public KeyVault(string clientId, string clientSecret, string vaultUrl)
            : this(new AuthenticationClient(clientId, clientSecret), vaultUrl) { }

        public KeyVault(string clientId, string certificateIdentifier, X509FindType identifierType, string vaultUrl, StoreLocation store = StoreLocation.LocalMachine)
           : this(new AuthenticationClient(clientId, certificateIdentifier, identifierType, store), vaultUrl) { }

        public KeyVault(string clientId, string vaultUrl)
           : this(new AuthenticationClient(clientId, clientId, X509FindType.FindBySubjectName), vaultUrl) { }

        public KeyVault(AuthenticationClient authClient, string vaultUrl)
        {
            if (vaultUrl != null)
                _vaultUrl = vaultUrl;

            _authClient = authClient;
            _client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback((a, r, s) => _authClient.GetTokenAsync(new[] { "https://vault.azure.net/.default" })));
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
