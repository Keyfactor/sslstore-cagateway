using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.SslStore.Interfaces;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class EnrollmentField : IEnrollmentField
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public int DataType { get; set; }
    }
}
