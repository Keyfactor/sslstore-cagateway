using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.SslStore.Interfaces;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class TemplateRegex : ITemplateRegex
    {
        public int TemplateId { get; set; }
        public string SubjectPart { get; set; }
        public string Regex { get; set; }
        public string Error { get; set; }
    }
}
