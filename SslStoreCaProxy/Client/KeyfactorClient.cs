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
            var keyfactorBaseUrl = new Uri(configProvider.CAConnectionData[Constants.KeyfactorApiUrl].ToString());
            var keyfactorAuth = configProvider.CAConnectionData[Constants.KeyfactorApiUserId] + ":" + configProvider.CAConnectionData[Constants.KeyfactorApiPassword];
            var plainTextBytes = Encoding.UTF8.GetBytes(keyfactorAuth);

            var clientHandler = new WebRequestHandler();
            RestClient = new HttpClient(clientHandler, true) { BaseAddress = keyfactorBaseUrl };
            RestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            RestClient.DefaultRequestHeaders.Add("x-keyfactor-requested-with", "APIClient");
            RestClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(plainTextBytes));
        }

        public async Task<Template> SubmitUpdateTemplateAsync(Template templateRequest)
        {
            using (var resp = await RestClient.PutAsync("/KeyfactorApi/Templates", new StringContent(
                JsonConvert.SerializeObject(templateRequest), Encoding.ASCII, "application/json")))
            {
                try
                {
                    Logger.Trace(JsonConvert.SerializeObject(templateRequest));
                    resp.EnsureSuccessStatusCode();
                    var templateResponse =
                        JsonConvert.DeserializeObject<Template>(await resp.Content.ReadAsStringAsync());
                    return templateResponse;
                }
                catch(Exception e)
                {
                    Logger.Error($"Keyfactor API Error Occured Updating Keyfactor Template: {e.Message}");
                    return new Template();
                }
                
            }
        }

        public async Task SubmitQueryTemplatesRequestAsync(BlockingCollection<ITemplate> bc, CancellationToken ct,
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
                                    $"5 consecutive failures to {resp.RequestMessage.RequestUri}");

                            continue;
                        }

                        var stringResponse = await resp.Content.ReadAsStringAsync();

                        var batchResponse =
                            JsonConvert.DeserializeObject<List<Template>>(stringResponse);

                        var batchCount = batchResponse.Count;

                        Logger.Trace($"Processing {batchCount} items in batch");
                        do
                        {
                            var r = batchResponse[batchItemsProcessed];
                            if (bc.TryAdd(r, 10, ct))
                            {
                                Logger.Trace($"Added Template ID {r.Id} to Queue for processing");
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


    }
}

