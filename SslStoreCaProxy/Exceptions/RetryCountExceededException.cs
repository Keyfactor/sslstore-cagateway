using System;

namespace Keyfactor.AnyGateway.SslStore.Exceptions
{
    public class RetryCountExceededException : Exception
    {
        public RetryCountExceededException(string message) : base(message)
        {
        }
    }
}