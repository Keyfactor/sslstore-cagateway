using System;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class QueryOrderRequest : IQueryOrderRequest
    {
        public AuthRequest AuthRequest { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CertificateExpireToDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CertificateExpireFromDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string DomainName { get; set; }

        [JsonProperty("SubUserID", NullValueHandling = NullValueHandling.Ignore)]
        public string SubUserId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ProductCode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string DateTimeCulture { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long PageNumber { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long PageSize { get; set; }
    }
}