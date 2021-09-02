namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IOrganizationAddress
    {
        string AddressLine1 { get; set; }
        string AddressLine2 { get; set; }
        string AddressLine3 { get; set; }
        string City { get; set; }
        string Region { get; set; }
        string PostalCode { get; set; }
        string Country { get; set; }
        string Phone { get; set; }
        string Fax { get; set; }
        string LocalityName { get; set; }
    }
}