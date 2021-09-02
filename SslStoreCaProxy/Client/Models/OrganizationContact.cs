
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class OrganizationContact : IOrganizationContact
    {
        public string Email { get; set; }
        public string Firstname { get; set; }
        public string JobTitle { get; set; }
        public string Lastname { get; set; }
        public string Phone { get; set; }
        [JsonProperty("Phone_Extension", NullValueHandling = NullValueHandling.Ignore)]
        public string PhoneExtension { get; set; }
        public string Username { get; set; }
    }
}
