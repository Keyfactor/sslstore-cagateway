using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IRevokeOrderRequest
    {
        AuthRequest AuthRequest { get; set; }
        string TheSslStoreOrderId { get; set; }
        string SerialNumber { get; set; }
    }
}