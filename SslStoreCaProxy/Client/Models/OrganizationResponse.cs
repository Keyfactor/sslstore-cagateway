using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.SslStore.Interfaces;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class OrganizationResponse : IOrganizationResponse
    {
        public AuthResponse AuthResponse { get; set; }
        public List<Organization> OrganizationList { get; set; }
    }
}
