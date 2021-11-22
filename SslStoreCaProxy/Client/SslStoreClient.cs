using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CAProxy.AnyGateway.Interfaces;
using CSS.Common.Logging;
using Keyfactor.AnyGateway.SslStore.Client.Models;
using Keyfactor.AnyGateway.SslStore.Exceptions;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyGateway.SslStore.Client

{
    public sealed class SslStoreClient : LoggingClientBase, ISslStoreClient
    {
        public SslStoreClient(ICAConnectorConfigProvider config)
        {
            if (config.CAConnectionData.ContainsKey(Constants.SslStoreUrl))
            {
                BaseUrl = new Uri(config.CAConnectionData[Constants.SslStoreUrl].ToString());
                RestClient = ConfigureRestClient();
            }
        }

        private Uri BaseUrl { get; }
        private HttpClient RestClient { get; }
        private int PageSize { get; } = 100;

        public async Task<NewOrderResponse> SubmitNewOrderRequestAsync(NewOrderRequest newOrderRequest)
        {
            using (var resp = await RestClient.PostAsync("/rest/order/neworder", new StringContent(
                JsonConvert.SerializeObject(newOrderRequest), Encoding.ASCII, "application/json")))
            {
                Logger.Trace(JsonConvert.SerializeObject(newOrderRequest));
                resp.EnsureSuccessStatusCode();
                var enrollmentResponse =
                    JsonConvert.DeserializeObject<NewOrderResponse>(await resp.Content.ReadAsStringAsync());
                return enrollmentResponse;
            }
        }

        public async Task<EmailApproverResponse> SubmitEmailApproverRequestAsync(EmailApproverRequest newApproverRequest)
        {
            using (var resp = await RestClient.PostAsync("/rest/order/approverlist", new StringContent(
                JsonConvert.SerializeObject(newApproverRequest), Encoding.ASCII, "application/json")))
            {
                Logger.Trace(JsonConvert.SerializeObject(newApproverRequest));
                resp.EnsureSuccessStatusCode();
                var enrollmentResponse =
                    JsonConvert.DeserializeObject<EmailApproverResponse>(await resp.Content.ReadAsStringAsync());
                return enrollmentResponse;
            }
        }

        public async Task<NewOrderResponse> SubmitReIssueRequestAsync(ReIssueRequest reIssueOrderRequest)
        {
            using (var resp = await RestClient.PostAsync("/rest/order/reissue", new StringContent(
                JsonConvert.SerializeObject(reIssueOrderRequest), Encoding.ASCII, "application/json")))
            {
                var orderStatusResponse =
                    JsonConvert.DeserializeObject<NewOrderResponse>(await resp.Content.ReadAsStringAsync());
                return orderStatusResponse;
            }
        }

        public async Task<NewOrderResponse> SubmitRenewRequestAsync(NewOrderRequest renewOrderRequest)
        {
            using (var resp = await RestClient.PostAsync("/rest/order/neworder", new StringContent(
                JsonConvert.SerializeObject(renewOrderRequest), Encoding.ASCII, "application/json")))
            {
                Logger.Trace(JsonConvert.SerializeObject(renewOrderRequest));
                resp.EnsureSuccessStatusCode();
                var enrollmentResponse =
                    JsonConvert.DeserializeObject<NewOrderResponse>(await resp.Content.ReadAsStringAsync());
                return enrollmentResponse;
            }
        }


        public async Task<IDownloadCertificateResponse> SubmitDownloadCertificateAsync(
            DownloadCertificateRequest downloadOrderRequest)
        {
            using (var resp = await RestClient.PostAsync("/rest/order/download", new StringContent(
                JsonConvert.SerializeObject(downloadOrderRequest), Encoding.ASCII, "application/json")))
            {
                Logger.Trace(JsonConvert.SerializeObject(downloadOrderRequest));
                resp.EnsureSuccessStatusCode();
                var downloadOrderResponse =
                    JsonConvert.DeserializeObject<DownloadCertificateResponse>(await resp.Content.ReadAsStringAsync());
                return downloadOrderResponse;
            }
        }

        public async Task SubmitQueryOrderRequestAsync(BlockingCollection<INewOrderResponse> bc, CancellationToken ct,
            RequestManager requestManager)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            try
            {
                var itemsProcessed = 0;
                var pageCounter = 0;
                var isComplete = false;
                var retryCount = 0;
                do
                {
                    pageCounter++;
                    var queryOrderRequest = requestManager.GetQueryOrderRequest(PageSize, pageCounter);
                    var batchItemsProcessed = 0;
                    using (var resp = await RestClient.PostAsync("/rest/order/query", new StringContent(
                        JsonConvert.SerializeObject(queryOrderRequest), Encoding.ASCII, "application/json")))
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            var responseMessage = resp.Content.ReadAsStringAsync().Result;
                            Logger.Error(
                                $"Failed Request to SslStore. Retrying request. Status Code {resp.StatusCode} | Message: {responseMessage}");
                            retryCount++;
                            if (retryCount > 5)
                                throw new RetryCountExceededException(
                                    $"5 consecutive failures to {resp.RequestMessage.RequestUri}");

                            continue;
                        }

                        var batchResponse =
                            JsonConvert.DeserializeObject<List<NewOrderResponse>>(
                                await resp.Content.ReadAsStringAsync());

                        Logger.Trace($"Order List JSON {JsonConvert.SerializeObject(batchResponse)}");
                        
                        var batchCount = batchResponse.Count;

                        Logger.Trace($"Processing {batchCount} items in batch");
                        do
                        {
                            var r = batchResponse[batchItemsProcessed];
                            if (bc.TryAdd(r, 10, ct))
                            {
                                Logger.Trace($"Added Certificate ID {r.TheSslStoreOrderId} to Queue for processing");
                                batchItemsProcessed++;
                                itemsProcessed++;
                                Logger.Trace($"Processed {batchItemsProcessed} of {batchCount}");
                                Logger.Trace($"Total Items Processed: {itemsProcessed}");
                            }
                            else
                            {
                                Logger.Trace($"Adding {r} blocked. Retry");
                            }
                        } while (batchItemsProcessed < batchCount); //batch loop
                    }

                    //assume that if we process less records than requested that we have reached the end of the certificate list
                    if (batchItemsProcessed < PageSize)
                        isComplete = true;
                } while (!isComplete); //page loop

                bc.CompleteAdding();
            }
            catch (OperationCanceledException cancelEx)
            {
                Logger.Warn($"Synchronize method was cancelled. Message: {cancelEx.Message}");
                bc.CompleteAdding();
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                // ReSharper disable once PossibleIntendedRethrow
                throw cancelEx;
            }
            catch (RetryCountExceededException retryEx)
            {
                Logger.Error($"Retries Failed: {retryEx.Message}");
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"HttpRequest Failed: {ex.Message}");
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }

        public async Task<IOrderStatusResponse> SubmitRevokeCertificateAsync(RevokeOrderRequest revokeOrderRequest)
        {
            using (var resp = await RestClient.PostAsync("/rest/order/refundrequest", new StringContent(
                JsonConvert.SerializeObject(revokeOrderRequest), Encoding.ASCII, "application/json")))
            {
                var revocationResponse =
                    JsonConvert.DeserializeObject<OrderStatusResponse>(await resp.Content.ReadAsStringAsync());
                return revocationResponse;
            }
        }

        public async Task<INewOrderResponse> SubmitOrderStatusRequestAsync(OrderStatusRequest orderStatusRequest)
        {
            using (var resp = await RestClient.PostAsync("/rest/order/status", new StringContent(
                JsonConvert.SerializeObject(orderStatusRequest), Encoding.ASCII, "application/json")))
            {
                var orderStatusResponse =
                    JsonConvert.DeserializeObject<NewOrderResponse>(await resp.Content.ReadAsStringAsync());
                return orderStatusResponse;
            }
        }

        public async Task<IOrganizationResponse> SubmitOrganizationListAsync(OrganizationListRequest organizationListRequest)
        {
            using (var resp = await RestClient.PostAsync("/rest/digicert/organizationlist", new StringContent(
                JsonConvert.SerializeObject(organizationListRequest), Encoding.ASCII, "application/json")))
            {
                var organizationListResponse =
                    JsonConvert.DeserializeObject<OrganizationResponse>(await resp.Content.ReadAsStringAsync());
                return organizationListResponse;
            }
        }

        private HttpClient ConfigureRestClient()
        {
            var clientHandler = new WebRequestHandler();
            var returnClient = new HttpClient(clientHandler, true) {BaseAddress = BaseUrl};
            returnClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return returnClient;
        }


    }
}