using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class AuthResponse : IAuthResponse
    {
        [JsonProperty("isError")] public bool IsError { get; set; }

        public List<string> Message { get; set; }
        public string Timestamp { get; set; }
        public string ReplayToken { get; set; }
        public string InvokingPartnerCode { get; set; }
    }
}