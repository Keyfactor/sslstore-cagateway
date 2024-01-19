using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class TemplateNewOrderRequest : ITemplateNewOrderRequest
    {
        public TemplateAuthRequest AuthRequest { get; set; }
        public ProductCode ProductCode { get; set; }
        public TemplateOrganizationInfo OrganizationInfo { get; set; }
        public ValidityPeriod ValidityPeriod { get; set; }
        public ServerCount ServerCount { get; set; }
        [JsonProperty("CSR")] public Csr Csr { get; set; }
        public DomainName DomainName { get; set; }
        public WebServerType WebServerType { get; set; }
        [JsonProperty("DNSNames")] public DnsNames DnsNames { get; set; }
        [JsonProperty("isCUOrder")] public IsCuOrder IsCuOrder { get; set; }
        [JsonProperty("AutoWWW", NullValueHandling = NullValueHandling.Ignore)] public AutoWWW AutoWWW { get; set; }
        [JsonProperty("isRenewalOrder")] public IsRenewalOrder IsRenewalOrder { get; set; }
        [JsonProperty("isTrialOrder")] public IsTrialOrder IsTrialOrder { get; set; }
        public TemplateAdminContact AdminContact { get; set; }
        public TemplateTechnicalContact TechnicalContact { get; set; }
        public ApproverEmail ApproverEmail { get; set; }
        [JsonProperty("FileAuthDVIndicator")] public FileAuthDvIndicator FileAuthDvIndicator { get; set; }
        [JsonProperty("CNAMEAuthDVIndicator")] public CNameAuthDvIndicator CNameAuthDvIndicator { get; set; }
        public SignatureHashAlgorithm SignatureHashAlgorithm { get; set; }
    }

    public class FieldData
    {
        public List<string> RequiredForProducts { get; set; }
        public string EnrollmentFieldMapping { get; set; }
        public bool Array { get; set; }
    }

    public class PartnerCode
    {
        public FieldData FieldData { get; set; }
    }

    public class AuthToken
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("AuthRequest")]
    public class TemplateAuthRequest
    {
        public PartnerCode PartnerCode { get; set; }
        public AuthToken AuthToken { get; set; }
    }

    public class ProductCode
    {
        public FieldData FieldData { get; set; }
    }

    public class OrganizationName
    {
        public FieldData FieldData { get; set; }
    }

    public class RegistrationNumber
    {
        public FieldData FieldData { get; set; }
    }

    public class JurisdictionCountry
    {
        public FieldData FieldData { get; set; }
    }

    public class AddressLine1
    {
        public FieldData FieldData { get; set; }
    }

    public class Region
    {
        public FieldData FieldData { get; set; }
    }

    public class PostalCode
    {
        public FieldData FieldData { get; set; }
    }

    public class LocalityName
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("OrganizationAddress")]
    public class TemplateOrganizationAddress
    {
        public AddressLine1 AddressLine1 { get; set; }
        public Region Region { get; set; }
        public PostalCode PostalCode { get; set; }
        public LocalityName LocalityName { get; set; }
        public Country Country { get; set; }
        public Phone Phone { get; set; }

    }

    [JsonObject("OrganizationInfo")]
    public class TemplateOrganizationInfo
    {
        public OrganizationName OrganizationName { get; set; }
        public RegistrationNumber RegistrationNumber { get; set; }
        public JurisdictionCountry JurisdictionCountry { get; set; }
        public TemplateOrganizationAddress OrganizationAddress { get; set; }
    }

    public class ValidityPeriod
    {
        public FieldData FieldData { get; set; }
    }

    public class ServerCount
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("CSR")]
    public class Csr
    {
        public FieldData FieldData { get; set; }
    }

    public class DomainName
    {
        public FieldData FieldData { get; set; }
    }

    public class WebServerType
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("DNSNames")]
    public class DnsNames
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("isCUOrder")]
    public class IsCuOrder
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("AutoWWW",ItemNullValueHandling =NullValueHandling.Ignore)]
    public class AutoWWW
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("isRenewalOrder")]
    public class IsRenewalOrder
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("isTrialOrder")]
    public class IsTrialOrder
    {
        public FieldData FieldData { get; set; }
    }

    public class FirstName
    {
        public FieldData FieldData { get; set; }
    }

    public class LastName
    {
        public FieldData FieldData { get; set; }
    }

    public class Phone
    {
        public FieldData FieldData { get; set; }
    }

    public class Email
    {
        public FieldData FieldData { get; set; }
    }

    public class Title
    {
        public FieldData FieldData { get; set; }
    }

    public class City
    {
        public FieldData FieldData { get; set; }
    }

    public class Country
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("AdminContact")]
    public class TemplateAdminContact
    {
        public FirstName FirstName { get; set; }
        public LastName LastName { get; set; }
        public Phone Phone { get; set; }
        public Email Email { get; set; }
        public Title Title { get; set; }
        public OrganizationName OrganizationName { get; set; }
        public AddressLine1 AddressLine1 { get; set; }
        public City City { get; set; }
        public Region Region { get; set; }
        public PostalCode PostalCode { get; set; }
        public Country Country { get; set; }
    }

    public class SubjectFirstName
    {
        public FieldData FieldData { get; set; }
    }

    public class SubjectLastName
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("TechnicalContact")]
    public class TemplateTechnicalContact
    {
        public FirstName FirstName { get; set; }
        public LastName LastName { get; set; }
        public SubjectFirstName SubjectFirstName { get; set; }
        public SubjectLastName SubjectLastName { get; set; }
        public Phone Phone { get; set; }
        public Email Email { get; set; }
        public Title Title { get; set; }
        public AddressLine1 AddressLine1 { get; set; }
        public City City { get; set; }
        public Region Region { get; set; }
        public PostalCode PostalCode { get; set; }
        public Country Country { get; set; }
    }

    public class ApproverEmail
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("FileAuthDVIndicator")]
    public class FileAuthDvIndicator
    {
        public FieldData FieldData { get; set; }
    }

    [JsonObject("CNAMEAuthDVIndicator")]
    public class CNameAuthDvIndicator
    {
        public FieldData FieldData { get; set; }
    }

    public class SignatureHashAlgorithm
    {
        public FieldData FieldData { get; set; }
    }
}