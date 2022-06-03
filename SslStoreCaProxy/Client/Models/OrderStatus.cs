using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class OrderStatus : IOrderStatus
    {
        [JsonProperty("isTinyOrder")] public bool IsTinyOrder { get; set; }

        [JsonProperty("isTinyOrderClaimed")] public bool IsTinyOrderClaimed { get; set; }

        public string MajorStatus { get; set; }
        public string MinorStatus { get; set; }
        public List<AuthenticationStatus> AuthenticationStatuses { get; set; }
        public string AuthenticationComments { get; set; }
        public List<OrderNote> OrderNotes { get; set; }
        public List<DomainAuthVettingStatus> DomainAuthVettingStatus { get; set; }
    }
}