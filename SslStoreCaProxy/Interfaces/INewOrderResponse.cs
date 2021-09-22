using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface INewOrderResponse
    {
        AdminContact AdminContact { get; set; }
        string ApproverEmail { get; set; }
        string AuthFileContent { get; set; }
        string AuthFileName { get; set; }
        AuthResponse AuthResponse { get; set; }
        string CnameAuthName { get; set; }
        string CnameAuthValue { get; set; }
        string CertificateEndDate { get; set; }
        string CertificateEndDateInUtc { get; set; }
        string CertificateStartDate { get; set; }
        string CertificateStartDateInUtc { get; set; }
        string CommonName { get; set; }
        string Country { get; set; }
        string CustomOrderId { get; set; }
        int CustomerId { get; set; }
        string CustomerLoginName { get; set; }
        string CustomerPassword { get; set; }
        string DnsNames { get; set; }
        string Duns { get; set; }
        string Locality { get; set; }
        string OrderAmount { get; set; }
        string OrderExpiryDate { get; set; }
        string OrderExpiryDateInUtc { get; set; }
        OrderStatus OrderStatus { get; set; }
        string Organization { get; set; }
        string OrganizationAddress { get; set; }
        string OrganizationPhone { get; set; }
        string OrganizationPostalcode { get; set; }
        string OrganizationalUnit { get; set; }
        string PartnerOrderId { get; set; }
        string PollDate { get; set; }
        string PollDateInUtc { get; set; }
        string PollStatus { get; set; }
        string ProductCode { get; set; }
        string ProductName { get; set; }
        string PurchaseDate { get; set; }
        string PurchaseDateInUtc { get; set; }
        string RefundRequestId { get; set; }
        string ReissueSuccessCode { get; set; }
        int SanCount { get; set; }
        string SerialNumber { get; set; }
        int ServerCount { get; set; }
        string SignatureEncryptionAlgorithm { get; set; }
        string SignatureHashAlgorithm { get; set; }
        string SiteSealurl { get; set; }
        string State { get; set; }
        string SubVendorName { get; set; }
        int TssOrganizationId { get; set; }
        TechnicalContact TechnicalContact { get; set; }
        string TheSslStoreOrderId { get; set; }
        string TinyOrderLink { get; set; }
        string Token { get; set; }
        string TokenCode { get; set; }
        string TokenId { get; set; }
        int Validity { get; set; }
        string VendorName { get; set; }
        string VendorOrderId { get; set; }
        string WebServerType { get; set; }
        bool IsRefundApproved { get; set; }
        int WildcardSanCount { get; set; }
    }
}