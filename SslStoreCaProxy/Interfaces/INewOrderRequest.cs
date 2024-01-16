using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface INewOrderRequest
    {
        AuthRequest AuthRequest { get; set; }
        string CustomOrderId { get; set; }
        string ProductCode { get; set; }
        string ExtraProductCodes { get; set; }
        OrganizationInfo OrganizationInfo { get; set; }
        long TssOrganizationId { get; set; }
        long ValidityPeriod { get; set; }
        long ServerCount { get; set; }
        string Csr { get; set; }
        string DomainName { get; set; }
        string WebServerType { get; set; }
        List<string> DnsNames { get; set; }
        bool IsCuOrder { get; set; }
        bool? AutoWWW { get; set; }
        bool IsRenewalOrder { get; set; }
        string SpecialInstructions { get; set; }
        string RelatedTheSslStoreOrderId { get; set; }
        bool IsTrialOrder { get; set; }
        AdminContact AdminContact { get; set; }
        TechnicalContact TechnicalContact { get; set; }
        string ApproverEmail { get; set; }
        long ReserveSanCount { get; set; }
        bool AddInstallationSupport { get; set; }
        string EmailLanguageCode { get; set; }
        bool? FileAuthDvIndicator { get; set; }
        bool? CnameAuthDvIndicator { get; set; }
        bool HttpsFileAuthDvIndicator { get; set; }
        string SignatureHashAlgorithm { get; set; }
        bool CertTransparencyIndicator { get; set; }
        long RenewalDays { get; set; }
        string DateTimeCulture { get; set; }
        string CsrUniqueValue { get; set; }
        bool IsPkcs10 { get; set; }
        long WildcardReserveSanCount { get; set; }
        string ProvisioningMethod { get; set; }
    }
}