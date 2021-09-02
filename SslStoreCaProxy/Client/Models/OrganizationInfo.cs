using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class OrganizationInfo : IOrganizationInfo
    {
        public string OrganizationName { get; set; }

        [JsonProperty("DUNS")] public string Duns { get; set; }

        public string Division { get; set; }
        public string IncorporatingAgency { get; set; }
        public string RegistrationNumber { get; set; }
        public string JurisdictionCity { get; set; }
        public string JurisdictionRegion { get; set; }
        public string JurisdictionCountry { get; set; }
        public string AssumedName { get; set; }
        public OrganizationAddress OrganizationAddress { get; set; }
    }
}