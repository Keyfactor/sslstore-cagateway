using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IOrganizationInfo
    {
        string OrganizationName { get; set; }
        string Duns { get; set; }
        string Division { get; set; }
        string IncorporatingAgency { get; set; }
        string RegistrationNumber { get; set; }
        string JurisdictionCity { get; set; }
        string JurisdictionRegion { get; set; }
        string JurisdictionCountry { get; set; }
        string AssumedName { get; set; }
        OrganizationAddress OrganizationAddress { get; set; }
    }
}