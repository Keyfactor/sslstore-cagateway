namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IOrganizationListRequest
    {
        string PartnerCode { get; set; }
        string AuthToken { get; set; }
    }
}