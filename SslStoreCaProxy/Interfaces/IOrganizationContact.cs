namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IOrganizationContact
    {
        string Email { get; set; }
        string Firstname { get; set; }
        string JobTitle { get; set; }
        string Lastname { get; set; }
        string Phone { get; set; }
        string PhoneExtension { get; set; }
        string Username { get; set; }
    }
}