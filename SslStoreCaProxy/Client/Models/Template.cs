using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class Template : ITemplate
    {
        public int Id { get; set; }
        public string CommonName { get; set; }
        public string TemplateName { get; set; }
        public string Oid { get; set; }
        public string KeySize { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string KeyType { get; set; }
        public string ForestRoot { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string FriendlyName { get; set; }
        public string KeyRetention { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? KeyRetentionDays { get; set; }
        public bool KeyArchival { get; set; }
        public List<EnrollmentField> EnrollmentFields { get; set; }
        public int AllowedEnrollmentTypes { get; set; }
        public List<TemplateRegex> TemplateRegexes { get; set; }
        public bool UseAllowedRequesters { get; set; }
        public List<string> AllowedRequesters { get; set; }
        public string DisplayName { get; set; }
    }
}
