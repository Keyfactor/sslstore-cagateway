using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    public sealed class KeyfactorClient: LoggingClientBase, IKeyfactorClient
    {
        private HttpClient RestClient { get; }
        private int PageSize { get; } = 100;

        public KeyfactorClient(ICAConnectorConfigProvider configProvider)
        {
            Logger.Trace("KeyfactorClient constructor called.");
            try
            {
                if (configProvider?.CAConnectionData == null)
                {
                    Logger.Error("KeyfactorClient: configProvider or CAConnectionData is null.");
                    throw new ArgumentNullException(nameof(configProvider), "configProvider or CAConnectionData is null.");
                }

                if (!configProvider.CAConnectionData.ContainsKey(Constants.KeyfactorApiUrl))
                {
                    Logger.Error($"KeyfactorClient: Missing required config key '{Constants.KeyfactorApiUrl}'.");
                    throw new InvalidOperationException($"Missing required config key '{Constants.KeyfactorApiUrl}'.");
                }

                var apiUrlValue = configProvider.CAConnectionData[Constants.KeyfactorApiUrl]?.ToString();
                Logger.Trace($"KeyfactorClient: KeyfactorApiUrl={apiUrlValue ?? "(null)"}");

                if (string.IsNullOrEmpty(apiUrlValue))
                {
                    Logger.Error("KeyfactorClient: KeyfactorApiUrl value is null or empty.");
                    throw new InvalidOperationException("KeyfactorApiUrl value is null or empty.");
                }

                var keyfactorBaseUrl = new Uri(apiUrlValue);

                var userId = configProvider.CAConnectionData.ContainsKey(Constants.KeyfactorApiUserId)
                    ? configProvider.CAConnectionData[Constants.KeyfactorApiUserId]?.ToString() ?? ""
                    : "";
                var password = configProvider.CAConnectionData.ContainsKey(Constants.KeyfactorApiPassword)
                    ? configProvider.CAConnectionData[Constants.KeyfactorApiPassword]?.ToString() ?? ""
                    : "";

                if (string.IsNullOrEmpty(userId))
                    Logger.Warn("KeyfactorClient: KeyfactorApiUserId is null or empty.");

                Logger.Trace($"KeyfactorClient: Configuring with userId={userId}, BaseAddress={keyfactorBaseUrl}");

                var keyfactorAuth = userId + ":" + password;
                var plainTextBytes = Encoding.UTF8.GetBytes(keyfactorAuth);

                var clientHandler = new WebRequestHandler();
                RestClient = new HttpClient(clientHandler, true) { BaseAddress = keyfactorBaseUrl };
                RestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                RestClient.DefaultRequestHeaders.Add("x-keyfactor-requested-with", "APIClient");
                RestClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(plainTextBytes));

                Logger.Trace("KeyfactorClient: RestClient configured successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"KeyfactorClient constructor failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<Template> SubmitUpdateTemplateAsync(Template templateRequest)
        {
            Logger.Trace("SubmitUpdateTemplateAsync called.");
            try
            {
                Logger.Trace($"SubmitUpdateTemplateAsync Request JSON: {JsonConvert.SerializeObject(templateRequest)}");
                using (var resp = await RestClient.PutAsync("/KeyfactorApi/Templates", new StringContent(
                    JsonConvert.SerializeObject(templateRequest), Encoding.ASCII, "application/json")))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync();
                    Logger.Trace($"SubmitUpdateTemplateAsync Response StatusCode={resp.StatusCode}, Body={responseBody}");
                    resp.EnsureSuccessStatusCode();
                    var templateResponse = JsonConvert.DeserializeObject<Template>(responseBody);
                    if (templateResponse == null)
                        Logger.Warn("SubmitUpdateTemplateAsync: Deserialized response is null.");
                    return templateResponse;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Keyfactor API Error Occurred Updating Keyfactor Template: {e.Message}\n{e.StackTrace}");
                if (e.InnerException != null)
                    Logger.Error($"Inner exception: {e.InnerException.Message}\n{e.InnerException.StackTrace}");
                return new Template();
            }
        }

        public async Task SubmitQueryTemplatesRequestAsync(BlockingCollection<ITemplate> bc, CancellationToken ct,
            RequestManager requestManager)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            Logger.Trace("SubmitQueryTemplatesRequestAsync starting...");
            try
            {
                var itemsProcessed = 0;
                var pageCounter = 0;
                var isComplete = false;
                var retryCount = 0;
                do
                {
                    pageCounter++;
                    Logger.Trace($"SubmitQueryTemplatesRequestAsync: Requesting page {pageCounter}");
                    var batchItemsProcessed = 0;
                    using (var resp = await RestClient.GetAsync("/KeyfactorApi/Templates"))
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            var responseMessage = resp.Content.ReadAsStringAsync().Result;
                            Logger.Error(
                                $"Failed Request to Keyfactor. Retrying request. Status Code {resp.StatusCode} | Message: {responseMessage}");
                            retryCount++;
                            if (retryCount > 5)
                                throw new RetryCountExceededException(
                                    $"5 consecutive failures to {resp.RequestMessage?.RequestUri}");

                            continue;
                        }

                        retryCount = 0;
                        var stringResponse = await resp.Content.ReadAsStringAsync();
                        Logger.Trace($"SubmitQueryTemplatesRequestAsync: Response length={stringResponse?.Length ?? 0}");

                        var batchResponse =
                            JsonConvert.DeserializeObject<List<Template>>(stringResponse);

                        if (batchResponse == null)
                        {
                            Logger.Warn("SubmitQueryTemplatesRequestAsync: Deserialized batch response is null. Ending pagination.");
                            isComplete = true;
                            break;
                        }

                        var batchCount = batchResponse.Count;
                        Logger.Trace($"Processing {batchCount} templates in batch");

                        if (batchCount == 0)
                        {
                            Logger.Trace("SubmitQueryTemplatesRequestAsync: Empty batch received. Ending pagination.");
                            isComplete = true;
                            break;
                        }

                        do
                        {
                            var r = batchResponse[batchItemsProcessed];
                            if (r == null)
                            {
                                Logger.Warn($"SubmitQueryTemplatesRequestAsync: Null template at index {batchItemsProcessed}, skipping.");
                                batchItemsProcessed++;
                                continue;
                            }

                            if (bc.TryAdd(r, 10, ct))
                            {
                                Logger.Trace($"Added Template ID {r.Id}, CommonName={r.CommonName ?? "(null)"} to Queue for processing");
                                batchItemsProcessed++;
                                itemsProcessed++;
                                Logger.Trace($"Processed {batchItemsProcessed} of {batchCount}");
                                Logger.Trace($"Total Items Processed: {itemsProcessed}");
                            }
                            else
                            {
                                Logger.Trace($"Adding template {r.Id} blocked. Retry");
                            }
                        } while (batchItemsProcessed < batchCount); //batch loop

                    }

                    //assume that if we process less records than requested that we have reached the end of the certificate list
                    if (batchItemsProcessed < PageSize)
                        isComplete = true;
                } while (!isComplete); //page loop

                Logger.Trace($"SubmitQueryTemplatesRequestAsync: Pagination complete. Total templates processed={itemsProcessed}");
                bc.CompleteAdding();
            }
            catch (OperationCanceledException cancelEx)
            {
                Logger.Warn($"SubmitQueryTemplatesRequestAsync was cancelled. Message: {cancelEx.Message}");
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
                Logger.Error($"SubmitQueryTemplatesRequestAsync unexpected error: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                bc.CompleteAdding();
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

        }


    }
}

