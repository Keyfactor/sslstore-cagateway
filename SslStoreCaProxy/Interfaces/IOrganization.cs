using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IOrganization
    {
        string Address { get; set; }
        string Address2 { get; set; }
        object ApproversContact { get; set; }
        string AssumedName { get; set; }
        string City { get; set; }
        string Country { get; set; }
        string Name { get; set; }
        OrganizationContact OrganizationContact { get; set; }
        string OrganizationPhone { get; set; }
        string State { get; set; }
        string Zip { get; set; }
        string Status { get; set; }
        int TssOrganizationId { get; set; }
        int VendorOrganizationId { get; set; }
    }
}