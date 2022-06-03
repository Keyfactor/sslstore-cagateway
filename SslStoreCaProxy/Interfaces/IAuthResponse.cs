using System.Collections.Generic;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IAuthResponse
    {
        bool IsError { get; set; }
        List<string> Message { get; set; }
        string Timestamp { get; set; }
        string ReplayToken { get; set; }
        string InvokingPartnerCode { get; set; }
    }
}