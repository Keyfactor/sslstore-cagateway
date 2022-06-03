using Keyfactor.AnyGateway.SslStore.Interfaces;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class AuthenticationStatus : IAuthenticationStatus
    {
        public string AuthenticationStep { get; set; }
        public string Status { get; set; }
        public string LastUpdated { get; set; }
    }
}