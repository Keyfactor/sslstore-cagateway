using System.Collections.Generic;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IRevokeOrderResponse
    {
        string InvokingPartnerCode { get; set; }
        List<string> Message { get; set; }
        object ReplayToken { get; set; }
        string Timestamp { get; set; }
        bool IsError { get; set; }
    }
}