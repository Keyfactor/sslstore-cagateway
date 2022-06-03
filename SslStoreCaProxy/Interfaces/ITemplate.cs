using System.Collections.Generic;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface ITemplate
    {
        int Id { get; set; }
        string CommonName { get; set; }
        string TemplateName { get; set; }
        string Oid { get; set; }
        string KeySize { get; set; }
        string KeyType { get; set; }
        string ForestRoot { get; set; }
        string FriendlyName { get; set; }
        string KeyRetention { get; set; }
        int? KeyRetentionDays { get; set; }
        bool KeyArchival { get; set; }
        List<EnrollmentField> EnrollmentFields { get; set; }
        int AllowedEnrollmentTypes { get; set; }
        List<TemplateRegex> TemplateRegexes { get; set; }
        bool UseAllowedRequesters { get; set; }
        List<string> AllowedRequesters { get; set; }
        string DisplayName { get; set; }
    }
}