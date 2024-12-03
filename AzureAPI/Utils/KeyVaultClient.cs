using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace SharedResources
{
    public class KeyVaultClient
    {
        private SecretClient _client { get; set; }
        private AuthenticationClient _authenticationClient { get; set; }

        private readonly string _vaultUrl;

        public string GetSecretValue(string secret) => _client.GetSecret(secret)?.Value?.Value;
        public T GetSecretValues<T>() => GetSecretValues().ToObject<T>();
        public JObject GetSecretValues()
        {
            var secretProperties = _client.GetPropertiesOfSecrets();
            var secrets = new JObject();
            foreach (var secret in secretProperties)
                secrets.SetFromPropertyPath(secret.Name, GetSecretValue(secret.Name));
            return secrets;
        }

        public KeyVaultClient(string clientId, string clientSecret, string vaultUrl)
            : this(new AuthenticationClient(clientId, clientSecret), vaultUrl) { }

        public KeyVaultClient(string clientId, string certificateIdentifier, X509FindType identifierType, string vaultUrl, StoreLocation store = StoreLocation.LocalMachine)
           : this(new AuthenticationClient(clientId, certificateIdentifier, identifierType, store), vaultUrl) { }

        public KeyVaultClient(string clientId, string vaultUrl)
           : this(new AuthenticationClient(clientId, clientId, X509FindType.FindBySubjectName), vaultUrl) { }

        public KeyVaultClient(string clientId, X509Certificate2 clientCertificate, string vaultUrl)
           : this(new AuthenticationClient(clientId, clientCertificate), vaultUrl) { }

        public KeyVaultClient(string vaultUrl)
           : this(new DefaultAzureCredential(), vaultUrl) { }

        public KeyVaultClient(TokenCredential authClient, string vaultUrl)
        {
            _vaultUrl = vaultUrl;
            _client = new SecretClient(new Uri(_vaultUrl), authClient);
        }

        public void AddKeyVaultConfiguration(IConfigurationBuilder builder, KeyVaultSecretManager keyVaultSecretManager = null)
            => builder.AddAzureKeyVault(_client, keyVaultSecretManager ?? new KeyVaultSecretManager());
    }

    public static class KeyVaultExtentions
    {
        public static void AddKeyVaultConfiguration(this IConfigurationBuilder builder,  IEnumerable<string> vaults, TokenCredential authClient = null, KeyVaultSecretManager keyVaultSecretManager = null)
        {
            if (keyVaultSecretManager == null) keyVaultSecretManager = new KeyVaultSecretManager();
            if (authClient == null) authClient = new DefaultAzureCredential();

            vaults
                ?.ForEach(vault =>
                {
                    var secretClient = new SecretClient(
                        new Uri($"https://{vault}.vault.azure.net/"),
                        authClient);
                    builder.AddAzureKeyVault(secretClient, keyVaultSecretManager);
                });
        }
    }
}
