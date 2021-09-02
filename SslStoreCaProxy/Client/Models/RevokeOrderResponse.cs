using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class RevokeOrderResponse : IRevokeOrderResponse
    {
        public string InvokingPartnerCode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Message { get; set; }

        public object ReplayToken { get; set; }
        public string Timestamp { get; set; }
        [JsonProperty("isError")] public bool IsError { get; set; }
    }
}