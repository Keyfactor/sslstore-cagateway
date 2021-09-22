using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IOrderStatusResponse
    {
        AuthResponse AuthResponse { get; set; }
        string PartnerOrderId { get; set; }
        string CustomOrderId { get; set; }
        string TheSslStoreOrderId { get; set; }
        string VendorOrderId { get; set; }
        string RefundRequestId { get; set; }
        bool IsRefundApproved { get; set; }
        string TinyOrderLink { get; set; }
        OrderStatus OrderStatus { get; set; }
        string OrderAmount { get; set; }
        string PurchaseDate { get; set; }
        string CertificateStartDate { get; set; }
        string CertificateEndDate { get; set; }
        string CommonName { get; set; }
        string DnsNames { get; set; }
        long SanCount { get; set; }
        long ServerCount { get; set; }
        long Validity { get; set; }
        string Organization { get; set; }
        string OrganizationalUnit { get; set; }
        string State { get; set; }
        string Country { get; set; }
        string Locality { get; set; }
        string OrganizationPhone { get; set; }
        string OrganizationAddress { get; set; }
        string OrganizationPostalcode { get; set; }
        string Duns { get; set; }
        string WebServerType { get; set; }
        string ApproverEmail { get; set; }
        string ProductName { get; set; }
        AdminContact AdminContact { get; set; }
        TechnicalContact TechnicalContact { get; set; }
        string ReissueSuccessCode { get; set; }
        string AuthFileName { get; set; }
        string AuthFileContent { get; set; }
        string PollStatus { get; set; }
        string PollDate { get; set; }
        string CustomerLoginName { get; set; }
        string CustomerPassword { get; set; }
        long CustomerId { get; set; }
        string TokenId { get; set; }
        string TokenCode { get; set; }
        string SiteSealurl { get; set; }
        string CnameAuthName { get; set; }
        string CnameAuthValue { get; set; }
        string SignatureEncryptionAlgorithm { get; set; }
        string SignatureHashAlgorithm { get; set; }
        string VendorName { get; set; }
        string SubVendorName { get; set; }
        string Token { get; set; }
        string CertificateStartDateInUtc { get; set; }
        string CertificateEndDateInUtc { get; set; }
        string PurchaseDateInUtc { get; set; }
        string PollDateInUtc { get; set; }
    }
}