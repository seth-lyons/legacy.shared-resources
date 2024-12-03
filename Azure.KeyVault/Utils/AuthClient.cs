using Azure.Core;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;


namespace SharedResources
{
    public class AuthClientTokenCredential : TokenCredential
    {
        public readonly AuthClient Client;
        public AuthClientTokenCredential(AuthClient client){
            Client = client;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => Client.GetToken(requestContext, cancellationToken);

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => await Client.GetTokenAsync(requestContext, cancellationToken);
    }
}
