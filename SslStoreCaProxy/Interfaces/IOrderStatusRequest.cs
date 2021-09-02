using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IOrderStatusRequest
    {
        AuthRequest AuthRequest { get; set; }
        string TheSslStoreOrderId { get; set; }
    }
}