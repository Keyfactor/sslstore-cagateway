using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.SslStore.Client.Models;

namespace Keyfactor.AnyGateway.SslStore.Interfaces
{
    public interface ISslStoreClient
    {
        Task<NewOrderResponse> SubmitNewOrderRequestAsync(NewOrderRequest newOrderRequest);

        Task<NewOrderResponse> SubmitReIssueRequestAsync(ReIssueRequest reIssueOrderRequest);

        Task<NewOrderResponse> SubmitRenewRequestAsync(NewOrderRequest renewOrderRequest);

        Task<IDownloadCertificateResponse> SubmitDownloadCertificateAsync(
            DownloadCertificateRequest downloadOrderRequest);

        Task SubmitQueryOrderRequestAsync(BlockingCollection<INewOrderResponse> bc, CancellationToken ct,
            RequestManager requestManager);

        Task<IOrderStatusResponse> SubmitRevokeCertificateAsync(RevokeOrderRequest revokeOrderRequest);

        Task<IOrganizationResponse> SubmitOrganizationListAsync(OrganizationListRequest organizationListRequest);

        Task<INewOrderResponse> SubmitOrderStatusRequestAsync(OrderStatusRequest orderStatusRequest);

        Task<EmailApproverResponse> SubmitEmailApproverRequestAsync(EmailApproverRequest newApproverRequest);
    }
}