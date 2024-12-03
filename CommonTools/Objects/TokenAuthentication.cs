using Newtonsoft.Json;
using System;

namespace SharedResources
{
    public class TokenAuthentication
    {
        private string _token { get; set; }

        [JsonProperty("access_token")]
        public string Token { get { return _token; } set { this.LastRequested = DateTime.UtcNow; this._token = value; } }

        [JsonProperty("token_type")]
        public string Type { get; set; }

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty("ext_expires_in")]
        public long ExtExpiresIn { get; set; }

        [JsonProperty("expires_on")]
        public string ExpiresOn { get; set; }

        [JsonProperty("not_before")]
        public string NotBefore { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("refresh_token_expires_in")]
        public int RefreshTokenExpiresIn { get; set; }

        [JsonProperty("owner_id")]
        public string Owner { get; set; }

        [JsonProperty("endpoint_id")]
        public string EndpointId { get; set; }

        public DateTime LastRequested { get; set; }
        public DateTime Expires => LastRequested.AddSeconds(ExpiresIn - 5);
        public DateTime RefreshTokenExpires => LastRequested.AddSeconds(RefreshTokenExpiresIn - 5);
        public bool Expired => DateTime.UtcNow >= Expires;
    }
}
