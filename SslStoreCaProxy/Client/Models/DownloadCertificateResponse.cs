using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class DownloadCertificateResponse : IDownloadCertificateResponse
    {
        public AuthResponse AuthResponse { get; set; }
        public string CertificateEndDate { get; set; }

        [JsonProperty("CertificateEndDateInUTC")]
        public string CertificateEndDateInUtc { get; set; }

        public string CertificateStartDate { get; set; }

        [JsonProperty("CertificateStartDateInUTC")]
        public string CertificateStartDateInUtc { get; set; }

        public string CertificateStatus { get; set; }
        public List<Certificate> Certificates { get; set; }
        [JsonProperty("PartnerOrderID")] public string PartnerOrderId { get; set; }
        [JsonProperty("TheSSLStoreOrderID")] public object TheSslStoreOrderId { get; set; }
        public string ValidationStatus { get; set; }
        [JsonProperty("VendorOrderID")] public object VendorOrderId { get; set; }
    }
}