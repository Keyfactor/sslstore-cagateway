namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IAuthenticationStatus
    {
        string AuthenticationStep { get; set; }
        string Status { get; set; }
        string LastUpdated { get; set; }
    }
}