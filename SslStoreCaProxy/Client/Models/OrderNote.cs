using Keyfactor.AnyGateway.SslStore.Interfaces;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class OrderNote : IOrderNote
    {
        public string Comments { get; set; }
        public string DateAdded { get; set; }
    }
}