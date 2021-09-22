using System;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IDomainAuthVettingStatus
    {
        string Domain { get; set; }
        string DcvMethod { get; set; }
        string DcvStatus { get; set; }
        string FileName { get; set; }
        string FileContents { get; set; }
        string DnsName { get; set; }
        string DnsEntry { get; set; }
        string PollStatus { get; set; }
        DateTime LastPollDate { get; set; }
    }
}