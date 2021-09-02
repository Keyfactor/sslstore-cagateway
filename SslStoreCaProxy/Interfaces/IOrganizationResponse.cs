using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IOrganizationResponse
    {
        AuthResponse AuthResponse { get; set; }
        List<Organization> OrganizationList { get; set; }
    }
}