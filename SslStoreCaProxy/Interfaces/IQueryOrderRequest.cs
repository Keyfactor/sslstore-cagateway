using System;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IQueryOrderRequest
    {
        AuthRequest AuthRequest { get; set; }
        DateTime? StartDate { get; set; }
        DateTime? EndDate { get; set; }
        DateTime? CertificateExpireToDate { get; set; }
        DateTime? CertificateExpireFromDate { get; set; }
        string DomainName { get; set; }
        string SubUserId { get; set; }
        string ProductCode { get; set; }
        string DateTimeCulture { get; set; }
        long PageNumber { get; set; }
        long PageSize { get; set; }
    }
}