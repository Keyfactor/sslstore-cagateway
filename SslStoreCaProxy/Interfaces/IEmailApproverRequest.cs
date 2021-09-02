using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IEmailApproverRequest
    {
        AuthRequest AuthRequest { get; set; }
        string ProductCode { get; set; }
        string DomainName { get; set; }
    }
}