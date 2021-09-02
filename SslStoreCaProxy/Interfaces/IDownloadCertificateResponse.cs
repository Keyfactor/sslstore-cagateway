using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IDownloadCertificateResponse
    {
        AuthResponse AuthResponse { get; set; }
        string CertificateEndDate { get; set; }
        string CertificateEndDateInUtc { get; set; }
        string CertificateStartDate { get; set; }
        string CertificateStartDateInUtc { get; set; }
        string CertificateStatus { get; set; }
        List<Certificate> Certificates { get; set; }
        string PartnerOrderId { get; set; }
        object TheSslStoreOrderId { get; set; }
        string ValidationStatus { get; set; }
        object VendorOrderId { get; set; }
    }
}