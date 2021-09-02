using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class AuthRequest : IAuthRequest
    {
        public string PartnerCode { get; set; }
        public string AuthToken { get; set; }
        public string ReplayToken { get; set; }
        public string UserAgent { get; set; }

        [JsonProperty("TokenID")] public string TokenId { get; set; }

        public string TokenCode { get; set; }

        [JsonProperty("IPAddress")] public string IpAddress { get; set; }

        public bool IsUsedForTokenSystem { get; set; }
        public string Token { get; set; }
    }
}