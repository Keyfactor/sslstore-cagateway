namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface ITemplateRegex
    {
        int TemplateId { get; set; }
        string SubjectPart { get; set; }
        string Regex { get; set; }
        string Error { get; set; }
    }
}