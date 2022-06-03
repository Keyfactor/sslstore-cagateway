using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IEmailApproverResponse
    {
        List<string> ApproverEmailList { get; set; }
        AuthResponse AuthResponse { get; set; }
        string BaseDomainName { get; set; }
    }
}