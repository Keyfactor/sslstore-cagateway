namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IAddSan
    {
        string OldValue { get; set; }
        string NewValue { get; set; }
    }
}