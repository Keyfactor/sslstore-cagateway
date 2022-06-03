using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface ITemplateNewOrderRequest
    {
        TemplateAuthRequest AuthRequest { get; set; }
        ProductCode ProductCode { get; set; }
        TemplateOrganizationInfo OrganizationInfo { get; set; }
        ValidityPeriod ValidityPeriod { get; set; }
        ServerCount ServerCount { get; set; }
        Csr Csr { get; set; }
        DomainName DomainName { get; set; }
        WebServerType WebServerType { get; set; }
        DnsNames DnsNames { get; set; }
        IsCuOrder IsCuOrder { get; set; }
        IsRenewalOrder IsRenewalOrder { get; set; }
        IsTrialOrder IsTrialOrder { get; set; }
        TemplateAdminContact AdminContact { get; set; }
        TemplateTechnicalContact TechnicalContact { get; set; }
        ApproverEmail ApproverEmail { get; set; }
        FileAuthDvIndicator FileAuthDvIndicator { get; set; }
        CNameAuthDvIndicator CNameAuthDvIndicator { get; set; }
        SignatureHashAlgorithm SignatureHashAlgorithm { get; set; }
    }
}