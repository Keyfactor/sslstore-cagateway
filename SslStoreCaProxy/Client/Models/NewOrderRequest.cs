using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class NewOrderRequest : INewOrderRequest
    {
        public AuthRequest AuthRequest { get; set; }

        [JsonProperty("CustomOrderID", NullValueHandling = NullValueHandling.Ignore)]
        public string CustomOrderId { get; set; }

        public string ProductCode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExtraProductCodes { get; set; }

        public OrganizationInfo OrganizationInfo { get; set; }

        [JsonProperty("TSSOrganizationId", NullValueHandling = NullValueHandling.Ignore)]
        public long TssOrganizationId { get; set; }

        public long ValidityPeriod { get; set; }

        public long ServerCount { get; set; }

        [JsonProperty("CSR")] public string Csr { get; set; }

        public string DomainName { get; set; }

        public string WebServerType { get; set; }

        [JsonProperty("DNSNames", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> DnsNames { get; set; }

        [JsonProperty("isCUOrder")] public bool IsCuOrder { get; set; }

        [JsonProperty("AutoWWW", NullValueHandling = NullValueHandling.Ignore)]
        public bool? AutoWWW { get; set; }

        [JsonProperty("isRenewalOrder")] public bool IsRenewalOrder { get; set; }

        public string SpecialInstructions { get; set; }

        [JsonProperty("RelatedTheSSLStoreOrderID", NullValueHandling = NullValueHandling.Ignore)]
        public string RelatedTheSslStoreOrderId { get; set; }

        [JsonProperty("isTrialOrder")] public bool IsTrialOrder { get; set; }

        public AdminContact AdminContact { get; set; }

        public TechnicalContact TechnicalContact { get; set; }

        public string ApproverEmail { get; set; }

        [JsonProperty("ReserveSANCount", NullValueHandling = NullValueHandling.Ignore)]
        public long ReserveSanCount { get; set; }

        public bool AddInstallationSupport { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EmailLanguageCode { get; set; }

        [JsonProperty("FileAuthDVIndicator")] public bool? FileAuthDvIndicator { get; set; }

        [JsonProperty("CNAMEAuthDVIndicator", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CnameAuthDvIndicator { get; set; }

        [JsonProperty("HTTPSFileAuthDVIndicator", NullValueHandling = NullValueHandling.Ignore)]
        public bool HttpsFileAuthDvIndicator { get; set; }

        public string SignatureHashAlgorithm { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool CertTransparencyIndicator { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long RenewalDays { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string DateTimeCulture { get; set; }

        [JsonProperty("CSRUniqueValue", NullValueHandling = NullValueHandling.Ignore)]
        public string CsrUniqueValue { get; set; }

        [JsonProperty("isPKCS10", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsPkcs10 { get; set; }

        [JsonProperty("WildcardReserveSANCount", NullValueHandling = NullValueHandling.Ignore)]
        public long WildcardReserveSanCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ProvisioningMethod { get; set; }
    }
}