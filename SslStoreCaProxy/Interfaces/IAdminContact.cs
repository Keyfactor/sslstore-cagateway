namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IAdminContact
    {
        string FirstName { get; set; }
        string LastName { get; set; }
        string SubjectFirstName { get; set; }
        string SubjectLastName { get; set; }
        string Phone { get; set; }
        string Fax { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string OrganizationName { get; set; }
        string AddressLine1 { get; set; }
        string AddressLine2 { get; set; }
        string City { get; set; }
        string Region { get; set; }
        string PostalCode { get; set; }
        string Country { get; set; }
    }
}