using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class OrderStatusRequest : IOrderStatusRequest
    {
        public AuthRequest AuthRequest { get; set; }
        [JsonProperty("TheSSLStoreOrderID")] public string TheSslStoreOrderId { get; set; }
    }
}