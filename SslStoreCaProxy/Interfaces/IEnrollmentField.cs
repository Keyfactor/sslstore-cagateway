using System.Collections.Generic;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IEnrollmentField
    {
        int Id { get; set; }
        string Name { get; set; }
        List<string> Options { get; set; }
        int DataType { get; set; }
    }
}