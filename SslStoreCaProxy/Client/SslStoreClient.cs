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
            Logger.Trace("SslStoreClient constructor called.");
            if (config?.CAConnectionData == null)
            {
                Logger.Error("SslStoreClient: config or CAConnectionData is null.");
                return;
            }

            if (config.CAConnectionData.ContainsKey(Constants.SslStoreUrl))
            {
                var urlValue = config.CAConnectionData[Constants.SslStoreUrl]?.ToString();
                Logger.Trace($"SslStoreClient: SslStoreUrl={urlValue ?? "(null)"}");
                if (string.IsNullOrEmpty(urlValue))
                {
                    Logger.Error("SslStoreClient: SslStoreUrl value is null or empty.");
                    return;
                }
                BaseUrl = new Uri(urlValue);
                RestClient = ConfigureRestClient();
                Logger.Trace("SslStoreClient: RestClient configured successfully.");
            }
            else
            {
                Logger.Error($"SslStoreClient: Missing required config key '{Constants.SslStoreUrl}'.");
            }
        }

        private Uri BaseUrl { get; }
        private HttpClient RestClient { get; }
        private int PageSize { get; } = 100;

        public async Task<NewOrderResponse> SubmitNewOrderRequestAsync(NewOrderRequest newOrderRequest)
        {
            Logger.Trace("SubmitNewOrderRequestAsync called.");
            try
            {
                Logger.Trace($"SubmitNewOrderRequestAsync Request JSON: {JsonConvert.SerializeObject(newOrderRequest)}");
                using (var resp = await RestClient.PostAsync("/rest/order/neworder", new StringContent(
                    JsonConvert.SerializeObject(newOrderRequest), Encoding.UTF8, "application/json")))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    Logger.Trace($"SubmitNewOrderRequestAsync Response StatusCode={resp.StatusCode}, Body={responseBody}");
                    resp.EnsureSuccessStatusCode();
                    var enrollmentResponse = JsonConvert.DeserializeObject<NewOrderResponse>(responseBody);
                    if (enrollmentResponse == null)
                        Logger.Warn("SubmitNewOrderRequestAsync: Deserialized response is null.");
                    return enrollmentResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SubmitNewOrderRequestAsync failed: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                throw;
            }
        }

        public async Task<EmailApproverResponse> SubmitEmailApproverRequestAsync(EmailApproverRequest newApproverRequest)
        {
            Logger.Trace("SubmitEmailApproverRequestAsync called.");
            try
            {
                Logger.Trace($"SubmitEmailApproverRequestAsync Request JSON: {JsonConvert.SerializeObject(newApproverRequest)}");
                using (var resp = await RestClient.PostAsync("/rest/order/approverlist", new StringContent(
                    JsonConvert.SerializeObject(newApproverRequest), Encoding.UTF8, "application/json")))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    Logger.Trace($"SubmitEmailApproverRequestAsync Response StatusCode={resp.StatusCode}, Body={responseBody}");
                    resp.EnsureSuccessStatusCode();
                    var enrollmentResponse = JsonConvert.DeserializeObject<EmailApproverResponse>(responseBody);
                    if (enrollmentResponse == null)
                        Logger.Warn("SubmitEmailApproverRequestAsync: Deserialized response is null.");
                    return enrollmentResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SubmitEmailApproverRequestAsync failed: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                throw;
            }
        }

        public async Task<NewOrderResponse> SubmitReIssueRequestAsync(ReIssueRequest reIssueOrderRequest)
        {
            Logger.Trace("SubmitReIssueRequestAsync called.");
            try
            {
                Logger.Trace($"SubmitReIssueRequestAsync Request JSON: {JsonConvert.SerializeObject(reIssueOrderRequest)}");
                using (var resp = await RestClient.PostAsync("/rest/order/reissue", new StringContent(
                    JsonConvert.SerializeObject(reIssueOrderRequest), Encoding.UTF8, "application/json")))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    Logger.Trace($"SubmitReIssueRequestAsync Response StatusCode={resp.StatusCode}, Body={responseBody}");
                    resp.EnsureSuccessStatusCode();
                    var orderStatusResponse = JsonConvert.DeserializeObject<NewOrderResponse>(responseBody);
                    if (orderStatusResponse == null)
                        Logger.Warn("SubmitReIssueRequestAsync: Deserialized response is null.");
                    return orderStatusResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SubmitReIssueRequestAsync failed: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                throw;
            }
        }

        public async Task<NewOrderResponse> SubmitRenewRequestAsync(NewOrderRequest renewOrderRequest)
        {
            Logger.Trace("SubmitRenewRequestAsync called.");
            try
            {
                Logger.Trace($"SubmitRenewRequestAsync Request JSON: {JsonConvert.SerializeObject(renewOrderRequest)}");
                using (var resp = await RestClient.PostAsync("/rest/order/neworder", new StringContent(
                    JsonConvert.SerializeObject(renewOrderRequest), Encoding.UTF8, "application/json")))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    Logger.Trace($"SubmitRenewRequestAsync Response StatusCode={resp.StatusCode}, Body={responseBody}");
                    resp.EnsureSuccessStatusCode();
                    var enrollmentResponse = JsonConvert.DeserializeObject<NewOrderResponse>(responseBody);
                    if (enrollmentResponse == null)
                        Logger.Warn("SubmitRenewRequestAsync: Deserialized response is null.");
                    return enrollmentResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SubmitRenewRequestAsync failed: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                throw;
            }
        }


        public async Task<IDownloadCertificateResponse> SubmitDownloadCertificateAsync(
            DownloadCertificateRequest downloadOrderRequest)
        {
            Logger.Trace("SubmitDownloadCertificateAsync called.");
            try
            {
                Logger.Trace($"SubmitDownloadCertificateAsync Request JSON: {JsonConvert.SerializeObject(downloadOrderRequest)}");
                using (var resp = await RestClient.PostAsync("/rest/order/download", new StringContent(
                    JsonConvert.SerializeObject(downloadOrderRequest), Encoding.UTF8, "application/json")))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    Logger.Trace($"SubmitDownloadCertificateAsync Response StatusCode={resp.StatusCode}, Body={responseBody}");
                    resp.EnsureSuccessStatusCode();
                    var downloadOrderResponse = JsonConvert.DeserializeObject<DownloadCertificateResponse>(responseBody);
                    if (downloadOrderResponse == null)
                        Logger.Warn("SubmitDownloadCertificateAsync: Deserialized response is null.");
                    return downloadOrderResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SubmitDownloadCertificateAsync failed: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                throw;
            }
        }

        public async Task SubmitQueryOrderRequestAsync(BlockingCollection<INewOrderResponse> bc, CancellationToken ct,
            RequestManager requestManager)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            Logger.Trace("SubmitQueryOrderRequestAsync starting...");
            try
            {
                var itemsProcessed = 0;
                var pageCounter = 0;
                var isComplete = false;
                var retryCount = 0;
                do
                {
                    pageCounter++;
                    Logger.Trace($"SubmitQueryOrderRequestAsync: Requesting page {pageCounter}, PageSize={PageSize}");
                    var queryOrderRequest = requestManager.GetQueryOrderRequest(PageSize, pageCounter);
                    var batchItemsProcessed = 0;
                    using (var resp = await RestClient.PostAsync("/rest/order/query", new StringContent(
                        JsonConvert.SerializeObject(queryOrderRequest), Encoding.UTF8, "application/json")))
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            var responseMessage = resp.Content.ReadAsStringAsync().Result;
                            Logger.Error(
                                $"Failed Request to SslStore. Retrying request. Status Code {resp.StatusCode} | Message: {responseMessage}");
                            retryCount++;
                            if (retryCount > 5)
                                throw new RetryCountExceededException(
                                    $"5 consecutive failures to {resp.RequestMessage?.RequestUri}");

                            continue;
                        }

                        retryCount = 0;
                        var responseBody = await resp.Content.ReadAsStringAsync();
                        Logger.Trace($"SubmitQueryOrderRequestAsync: Page {pageCounter} response length={responseBody?.Length ?? 0}");

                        var batchResponse =
                            JsonConvert.DeserializeObject<List<NewOrderResponse>>(responseBody);

                        if (batchResponse == null)
                        {
                            Logger.Warn($"SubmitQueryOrderRequestAsync: Deserialized batch response is null for page {pageCounter}. Ending pagination.");
                            isComplete = true;
                            break;
                        }

                        Logger.Trace($"Order List JSON {JsonConvert.SerializeObject(batchResponse)}");

                        var batchCount = batchResponse.Count;
                        Logger.Trace($"Processing {batchCount} items in batch (page {pageCounter})");

                        if (batchCount == 0)
                        {
                            Logger.Trace("SubmitQueryOrderRequestAsync: Empty batch received. Ending pagination.");
                            isComplete = true;
                            break;
                        }

                        do
                        {
                            var r = batchResponse[batchItemsProcessed];
                            if (r == null)
                            {
                                Logger.Warn($"SubmitQueryOrderRequestAsync: Null item at index {batchItemsProcessed} in batch, skipping.");
                                batchItemsProcessed++;
                                continue;
                            }

                            if (bc.TryAdd(r, 10, ct))
                            {
                                Logger.Trace($"Added Certificate ID {r.TheSslStoreOrderId ?? "(null)"} to Queue for processing");
                                batchItemsProcessed++;
                                itemsProcessed++;
                                Logger.Trace($"Processed {batchItemsProcessed} of {batchCount}");
                                Logger.Trace($"Total Items Processed: {itemsProcessed}");
                            }
                            else
                            {
                                Logger.Trace($"Adding order {r.TheSslStoreOrderId ?? "(null)"} blocked. Retry");
                            }
                        } while (batchItemsProcessed < batchCount); //batch loop
                    }

                    //assume that if we process less records than requested that we have reached the end of the certificate list
                    if (batchItemsProcessed < PageSize)
                        isComplete = true;
                } while (!isComplete); //page loop

                Logger.Trace($"SubmitQueryOrderRequestAsync: Pagination complete. Total items processed={itemsProcessed}, pages={pageCounter}");
                bc.CompleteAdding();
            }
            catch (OperationCanceledException cancelEx)
            {
                Logger.Warn($"Synchronize method was cancelled. Message: {cancelEx.Message}");
                bc.CompleteAdding();
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                throw;
            }
            catch (RetryCountExceededException retryEx)
            {
                Logger.Error($"Retries Failed: {retryEx.Message}\n{retryEx.StackTrace}");
                bc.CompleteAdding();
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"HttpRequest Failed: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                bc.CompleteAdding();
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Error($"SubmitQueryOrderRequestAsync unexpected error: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                bc.CompleteAdding();
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }

        public async Task<IOrderStatusResponse> SubmitRevokeCertificateAsync(RevokeOrderRequest revokeOrderRequest)
        {
            Logger.Trace("SubmitRevokeCertificateAsync called.");
            try
            {
                Logger.Trace($"SubmitRevokeCertificateAsync Request JSON: {JsonConvert.SerializeObject(revokeOrderRequest)}");
                using (var resp = await RestClient.PostAsync("/rest/order/refundrequest", new StringContent(
                    JsonConvert.SerializeObject(revokeOrderRequest), Encoding.UTF8, "application/json")))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    Logger.Trace($"SubmitRevokeCertificateAsync Response StatusCode={resp.StatusCode}, Body={responseBody}");
                    var revocationResponse = JsonConvert.DeserializeObject<OrderStatusResponse>(responseBody);
                    if (revocationResponse == null)
                        Logger.Warn("SubmitRevokeCertificateAsync: Deserialized response is null.");
                    return revocationResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SubmitRevokeCertificateAsync failed: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                throw;
            }
        }

        public async Task<INewOrderResponse> SubmitOrderStatusRequestAsync(OrderStatusRequest orderStatusRequest)
        {
            Logger.Trace("SubmitOrderStatusRequestAsync called.");
            try
            {
                Logger.Trace($"SubmitOrderStatusRequestAsync Request JSON: {JsonConvert.SerializeObject(orderStatusRequest)}");
                using (var resp = await RestClient.PostAsync("/rest/order/status", new StringContent(
                    JsonConvert.SerializeObject(orderStatusRequest), Encoding.UTF8, "application/json")))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    Logger.Trace($"SubmitOrderStatusRequestAsync Response StatusCode={resp.StatusCode}, Body={responseBody}");
                    var orderStatusResponse = JsonConvert.DeserializeObject<NewOrderResponse>(responseBody);
                    if (orderStatusResponse == null)
                        Logger.Warn("SubmitOrderStatusRequestAsync: Deserialized response is null.");
                    return orderStatusResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SubmitOrderStatusRequestAsync failed: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                throw;
            }
        }

        public async Task<IOrganizationResponse> SubmitOrganizationListAsync(OrganizationListRequest organizationListRequest)
        {
            Logger.Trace("SubmitOrganizationListAsync called.");
            try
            {
                Logger.Trace($"SubmitOrganizationListAsync Request JSON: {JsonConvert.SerializeObject(organizationListRequest)}");
                using (var resp = await RestClient.PostAsync("/rest/digicert/organizationlist", new StringContent(
                    JsonConvert.SerializeObject(organizationListRequest), Encoding.UTF8, "application/json")))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    Logger.Trace($"SubmitOrganizationListAsync Response StatusCode={resp.StatusCode}, Body={responseBody}");
                    var organizationListResponse = JsonConvert.DeserializeObject<OrganizationResponse>(responseBody);
                    if (organizationListResponse == null)
                        Logger.Warn("SubmitOrganizationListAsync: Deserialized response is null.");
                    return organizationListResponse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SubmitOrganizationListAsync failed: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                throw;
            }
        }

        private HttpClient ConfigureRestClient()
        {
            var clientHandler = new WebRequestHandler();
            var returnClient = new HttpClient(clientHandler, true) { BaseAddress = BaseUrl };
            returnClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return returnClient;
        }


    }
}