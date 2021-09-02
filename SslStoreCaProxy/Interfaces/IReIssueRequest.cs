using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IReIssueRequest
    {
        AuthRequest AuthRequest { get; set; }
        string TheSslStoreOrderId { get; set; }
        string Csr { get; set; }
        string WebServerType { get; set; }
        List<string> DnsNames { get; set; }
        bool IsRenewalOrder { get; set; }
        string SpecialInstructions { get; set; }
        List<EditSan> EditSan { get; set; }
        List<DeleteSan> DeleteSan { get; set; }
        List<AddSan> AddSan { get; set; }
        bool IsWildCard { get; set; }
        string ReissueEmail { get; set; }
        bool PreferEnrollmentLink { get; set; }
        string SignatureHashAlgorithm { get; set; }
        bool FileAuthDvIndicator { get; set; }
        bool HttpsFileAuthDvIndicator { get; set; }
        bool CNameAuthDvIndicator { get; set; }
        string ApproverEmails { get; set; }
        string DateTimeCulture { get; set; }
        string CsrUniqueValue { get; set; }
    }
}