using System;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class DomainAuthVettingStatus : IDomainAuthVettingStatus
    {
        [JsonProperty("domain")] public string Domain { get; set; }

        [JsonProperty("dcvMethod")] public string DcvMethod { get; set; }

        [JsonProperty("dcvStatus")] public string DcvStatus { get; set; }

        public string FileName { get; set; }
        public string FileContents { get; set; }

        [JsonProperty("DNSName")] public string DnsName { get; set; }

        [JsonProperty("DNSEntry")] public string DnsEntry { get; set; }

        public string PollStatus { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime LastPollDate { get; set; }
    }
}