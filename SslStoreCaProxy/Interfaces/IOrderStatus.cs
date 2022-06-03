using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IOrderStatus
    {
        bool IsTinyOrder { get; set; }
        bool IsTinyOrderClaimed { get; set; }
        string MajorStatus { get; set; }
        string MinorStatus { get; set; }
        List<AuthenticationStatus> AuthenticationStatuses { get; set; }
        string AuthenticationComments { get; set; }
        List<OrderNote> OrderNotes { get; set; }
        List<DomainAuthVettingStatus> DomainAuthVettingStatus { get; set; }
    }
}