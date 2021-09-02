using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class EmailApproverResponse : IEmailApproverResponse
    {
        public List<string> ApproverEmailList { get; set; }
        public AuthResponse AuthResponse { get; set; }
        [JsonProperty("baseDomainName", NullValueHandling = NullValueHandling.Ignore)]
        public string BaseDomainName { get; set; }
    }
}
