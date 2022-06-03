namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IAuthRequest
    {
        string PartnerCode { get; set; }
        string AuthToken { get; set; }
        string ReplayToken { get; set; }
        string UserAgent { get; set; }
        string TokenId { get; set; }
        string TokenCode { get; set; }
        string IpAddress { get; set; }
        bool IsUsedForTokenSystem { get; set; }
        string Token { get; set; }
    }
}