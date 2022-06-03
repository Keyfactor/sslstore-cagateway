using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class Organization : IOrganization
    {
        public string Address { get; set; }
        public string Address2 { get; set; }
        public object ApproversContact { get; set; }
        public string AssumedName { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Name { get; set; }
        public OrganizationContact OrganizationContact { get; set; }
        [JsonProperty("Organization_Phone", NullValueHandling = NullValueHandling.Ignore)]
        public string OrganizationPhone { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Status { get; set; }
        [JsonProperty("TSSOrganizationId", NullValueHandling = NullValueHandling.Ignore)]
        public int TssOrganizationId { get; set; }
        public int VendorOrganizationId { get; set; }
    }
}
