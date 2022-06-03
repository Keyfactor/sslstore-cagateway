using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class ReIssueRequest : IReIssueRequest
    {
        public AuthRequest AuthRequest { get; set; }
        [JsonProperty("TheSSLStoreOrderID")] public string TheSslStoreOrderId { get; set; }
        [JsonProperty("CSR")] public string Csr { get; set; }
        public string WebServerType { get; set; }
        [JsonProperty("DNSNames")] public List<string> DnsNames { get; set; }
        [JsonProperty("isRenewalOrder")] public bool IsRenewalOrder { get; set; }
        public string SpecialInstructions { get; set; }
        [JsonProperty("EditSAN")] public List<EditSan> EditSan { get; set; }
        [JsonProperty("DeleteSAN")] public List<DeleteSan> DeleteSan { get; set; }
        [JsonProperty("AddSAN")] public List<AddSan> AddSan { get; set; }
        [JsonProperty("isWildCard")] public bool IsWildCard { get; set; }
        public string ReissueEmail { get; set; }
        public bool PreferEnrollmentLink { get; set; }
        public string SignatureHashAlgorithm { get; set; }
        [JsonProperty("FileAuthDVIndicator")] public bool FileAuthDvIndicator { get; set; }

        [JsonProperty("HTTPSFileAuthDVIndicator")]
        public bool HttpsFileAuthDvIndicator { get; set; }

        [JsonProperty("CNAMEAuthDVIndicator")] public bool CNameAuthDvIndicator { get; set; }
        public string ApproverEmails { get; set; }
        public string DateTimeCulture { get; set; }
        [JsonProperty("CSRUniqueValue")] public string CsrUniqueValue { get; set; }
    }
}