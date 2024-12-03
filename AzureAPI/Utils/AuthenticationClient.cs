using Azure.Core;
using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SharedResources
{
    public class AuthenticationClient: TokenCredential
    {
        protected IConfidentialClientApplication _appAuthentication;
        protected string _clientId;
        protected string _clientSecret;
        protected X509Certificate2 _clientCertificate;
        protected string[] _scope;

        public AuthenticationClient(string clientId, string clientSecret, string[] defaultScope = null)
            : this(clientId, clientSecret, null, defaultScope ?? new[] { $"api://{clientId}/.default" }) { }

        public AuthenticationClient(string clientId, X509Certificate2 clientCertificate, string[] defaultScope = null)
            : this(clientId, null, clientCertificate, defaultScope ?? new[] { $"api://{clientId}/.default" }) { }

        public AuthenticationClient(string clientId, string certificateIdentifier, X509FindType identifierType, StoreLocation storeLocation = StoreLocation.LocalMachine, string[] defaultScope = null)
            : this(clientId, null, Operations.GetCertificate(certificateIdentifier, identifierType, storeLocation), defaultScope ?? new[] { $"api://{clientId}/.default" }) { }

        private AuthenticationClient(string clientId, string clientSecret, X509Certificate2 clientCertificate, string[] defaultScope)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _clientCertificate = clientCertificate;
            _scope = defaultScope;

            var builder = ConfidentialClientApplicationBuilder
                .Create(_clientId)
                .WithTenantId("7fff43ef-4b23-4929-9880-b9d71c748c64");
            if (_clientCertificate != null)
                builder.WithCertificate(_clientCertificate);
            else
                builder.WithClientSecret(_clientSecret);

            _appAuthentication = builder.Build();
        }

        public async Task<string> GetTokenAsync(string scope) => await GetTokenAsync(new[] { scope });

        public async Task<string> GetTokenAsync(string[] scope = null)
        {
            AuthenticationResult result = await _appAuthentication
                ?.AcquireTokenForClient(scope ?? _scope)
                ?.ExecuteAsync();
            return result?.AccessToken;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => GetTokenAsync(requestContext, cancellationToken).Result;

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await _appAuthentication.AcquireTokenForClient(requestContext.Scopes ?? _scope).ExecuteAsync();
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }
    }
}
