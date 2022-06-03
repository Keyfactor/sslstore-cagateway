using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.SslStore.Interfaces;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class OrganizationListRequest : IOrganizationListRequest
    {
        public string PartnerCode { get; set; }
        public string AuthToken { get; set; }
    }
}
