using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class NewOrderResponse : INewOrderResponse
    {
        public AdminContact AdminContact { get; set; }
        public string ApproverEmail { get; set; }
        public string AuthFileContent { get; set; }
        public string AuthFileName { get; set; }
        public AuthResponse AuthResponse { get; set; }
        [JsonProperty("CNAMEAuthName")] public string CnameAuthName { get; set; }
        [JsonProperty("CNAMEAuthValue")] public string CnameAuthValue { get; set; }
        public string CertificateEndDate { get; set; }

        [JsonProperty("CertificateEndDateInUTC")]
        public string CertificateEndDateInUtc { get; set; }

        public string CertificateStartDate { get; set; }

        [JsonProperty("CertificateStartDateInUTC")]
        public string CertificateStartDateInUtc { get; set; }

        public string CommonName { get; set; }
        public string Country { get; set; }
        [JsonProperty("CustomOrderID")] public string CustomOrderId { get; set; }
        [JsonProperty("CustomerID")] public int CustomerId { get; set; }
        public string CustomerLoginName { get; set; }
        public string CustomerPassword { get; set; }
        [JsonProperty("DNSNames")] public string DnsNames { get; set; }
        [JsonProperty("DUNS")] public string Duns { get; set; }
        public string Locality { get; set; }
        public string OrderAmount { get; set; }
        public string OrderExpiryDate { get; set; }
        [JsonProperty("OrderExpiryDateInUTC")] public string OrderExpiryDateInUtc { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public string Organization { get; set; }
        public string OrganizationAddress { get; set; }
        public string OrganizationPhone { get; set; }
        public string OrganizationPostalcode { get; set; }
        public string OrganizationalUnit { get; set; }
        [JsonProperty("PartnerOrderID")] public string PartnerOrderId { get; set; }
        public string PollDate { get; set; }
        [JsonProperty("PollDateInUTC")] public string PollDateInUtc { get; set; }
        public string PollStatus { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string PurchaseDate { get; set; }
        [JsonProperty("PurchaseDateInUTC")] public string PurchaseDateInUtc { get; set; }
        [JsonProperty("RefundRequestID")] public string RefundRequestId { get; set; }
        public string ReissueSuccessCode { get; set; }
        [JsonProperty("SANCount")] public int SanCount { get; set; }
        public string SerialNumber { get; set; }
        public int ServerCount { get; set; }
        public string SignatureEncryptionAlgorithm { get; set; }
        public string SignatureHashAlgorithm { get; set; }
        public string SiteSealurl { get; set; }
        public string State { get; set; }
        public string SubVendorName { get; set; }
        [JsonProperty("TSSOrganizationId")] public int TssOrganizationId { get; set; }
        public TechnicalContact TechnicalContact { get; set; }
        [JsonProperty("TheSSLStoreOrderID")] public string TheSslStoreOrderId { get; set; }
        public string TinyOrderLink { get; set; }
        public string Token { get; set; }
        public string TokenCode { get; set; }
        [JsonProperty("TokenID")] public string TokenId { get; set; }
        public int Validity { get; set; }
        public string VendorName { get; set; }
        [JsonProperty("VendorOrderID")] public string VendorOrderId { get; set; }
        public string WebServerType { get; set; }
        [JsonProperty("isRefundApproved")] public bool IsRefundApproved { get; set; }
        [JsonProperty("wildcardSANCount")] public int WildcardSanCount { get; set; }
    }
}