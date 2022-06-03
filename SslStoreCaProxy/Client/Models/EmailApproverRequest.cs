using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.SslStore.Interfaces;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class EmailApproverRequest : IEmailApproverRequest
    {
        public AuthRequest AuthRequest { get; set; }
        public string ProductCode { get; set; }
        public string DomainName { get; set; }
    }
}
