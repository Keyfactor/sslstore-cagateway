using Keyfactor.AnyGateway.SslStore.Interfaces;

namespace Keyfactor.AnyGateway.SslStore.Client.Models
{
    public class AddSan : IAddSan
    {
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }
}