using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface IKeyfactorClient
    {
        Task SubmitQueryTemplatesRequestAsync(BlockingCollection<ITemplate> bc, CancellationToken ct,
            RequestManager requestManager);

        Task<Template> SubmitUpdateTemplateAsync(Template templateRequest);
    }
}