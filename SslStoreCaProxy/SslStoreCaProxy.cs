using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CAProxy.AnyGateway;
using CAProxy.AnyGateway.Interfaces;
using CAProxy.AnyGateway.Models;
using CAProxy.Common;
using CSS.Common;
using CSS.Common.Logging;
using CSS.PKI;
using Keyfactor.AnyGateway.SslStore.Client;
using Keyfactor.AnyGateway.SslStore.Client.Models;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using System.Linq;

namespace Keyfactor.AnyGateway.SslStore
{
    public class SslStoreCaProxy : BaseCAConnector
    {
        private readonly RequestManager _requestManager;

        public SslStoreCaProxy()
        {
            _requestManager = new RequestManager(this);
        }

        private IKeyfactorClient KeyfactorClient { get; set; }
        private ISslStoreClient SslStoreClient { get; set; }
        private ICAConnectorConfigProvider ConfigManager { get; set; }
        public string PartnerCode { get; set; }
        public string AuthenticationToken { get; set; }
        public int PageSize { get; set; }

        public override int Revoke(string caRequestId, string hexSerialNumber, uint revocationReason)
        {
            var revokeOrderRequest = _requestManager.GetRevokeOrderRequest(caRequestId.Split('-')[0]);

            try
            {
                var requestResponse =
                    Task.Run(async () => await SslStoreClient.SubmitRevokeCertificateAsync(revokeOrderRequest)).Result;

                if (requestResponse.AuthResponse.IsError)
                {
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                    return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
                }

                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

                return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.REVOKED);
            }
            catch (Exception e)
            {
                Logger.Error($"An Error has occurred during the revoke process {e.Message}");
                return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
            }
        }

        [Obsolete]
        public override EnrollmentResult Enroll(string csr, string subject, Dictionary<string, string[]> san,
            EnrollmentProductInfo productInfo,
            PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
        {
            return new EnrollmentResult();
        }

        public override EnrollmentResult Enroll(ICertificateDataReader certificateDataReader, string csr,
            string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo,
            PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            INewOrderResponse enrollmentResponse = null;
            NewOrderRequest enrollmentRequest;
            ReIssueRequest reIssueRequest;
            CAConnectorCertificate priorCert;
            OrderStatusRequest orderStatusRequest;
            INewOrderResponse orderStatusResponse;

            switch (enrollmentType)
            {
                case RequestUtilities.EnrollmentType.New:
                    //If they renewed an expired cert it gets here and this will not be supported
                    if (!productInfo.ProductParameters.ContainsKey("PriorCertSN"))
                    {
                       string[] arrayProducts= Array.Empty<string>();
                       string[] arrayApproverEmails= Array.Empty<string>();

                        if (productInfo.ProductParameters.ContainsKey("DNS Names Comma Separated"))
                        { 
                            arrayProducts = productInfo.ProductParameters["DNS Names Comma Separated"].Split(new char[] { ',' }); 
                        }
                        if(productInfo.ProductParameters.ContainsKey("Approver Email"))
                        {
                            arrayApproverEmails = productInfo.ProductParameters["Approver Email"].Split(new char[] { ',' });
                        }

                        var count = 1;
                        foreach (var product in arrayProducts)
                        {
                            
                            var emailApproverRequest = _requestManager.GetEmailApproverListRequest(productInfo.ProductID, product);
                            var emailApproverResponse = Task.Run(async () =>
                                await SslStoreClient.SubmitEmailApproverRequestAsync(emailApproverRequest));
                            
                            var emailValidation=ValidateEmails(emailApproverResponse, arrayApproverEmails, productInfo,count);

                            if(emailValidation.Length>0)
                            {
                                return new EnrollmentResult
                                {
                                    Status = 30, //failure
                                    StatusMessage = emailValidation
                                };
                            }
                            count++;
                        }
                       

                        enrollmentRequest =
                            _requestManager.GetEnrollmentRequest(csr, productInfo, ConfigManager, false);
                        enrollmentResponse =
                            Task.Run(async () => await SslStoreClient.SubmitNewOrderRequestAsync(enrollmentRequest))
                                .Result;
                    }
                    else
                    {
                        return new EnrollmentResult
                        {
                            Status = 30, //failure
                            StatusMessage = "You cannot renew and expired cert please perform an new enrollment."
                        };
                    }

                    break;
                case RequestUtilities.EnrollmentType.Renew:
                    priorCert = certificateDataReader.GetCertificateRecord(
                        DataConversion.HexToBytes(productInfo.ProductParameters["PriorCertSN"]));
                    orderStatusRequest = _requestManager.GetOrderStatusRequest(priorCert.CARequestID.Split('-')[0]);
                    orderStatusResponse = Task.Run(async () =>
                        await SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)).Result;
                    var renewRequest = _requestManager.GetRenewalRequest(orderStatusResponse, csr);
                    enrollmentResponse =
                        Task.Run(async () => await SslStoreClient.SubmitRenewRequestAsync(renewRequest)).Result;
                    break;
                case RequestUtilities.EnrollmentType.Reissue:
                    priorCert =
                        certificateDataReader.GetCertificateRecord(
                            DataConversion.HexToBytes(productInfo.ProductParameters["PriorCertSN"]));
                    orderStatusRequest = _requestManager.GetOrderStatusRequest(priorCert.CARequestID.Split('-')[0]);
                    orderStatusResponse = Task.Run(async () =>
                        await SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)).Result;
                    reIssueRequest = _requestManager.GetReIssueRequest(orderStatusResponse, csr, false);

                    enrollmentResponse =
                        Task.Run(async () => await SslStoreClient.SubmitReIssueRequestAsync(reIssueRequest)).Result;
                    break;
            }

            return GetEnrollmentResult(enrollmentResponse);
        }

        private EnrollmentResult GetEnrollmentResult(INewOrderResponse newOrderResponse)
        {
            if (newOrderResponse != null && newOrderResponse.AuthResponse.IsError)
            {
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                return new EnrollmentResult
                {
                    Status = 30, //failure
                    StatusMessage = newOrderResponse.AuthResponse.Message[0]
                };
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            return new EnrollmentResult
            {
                Status = 9, //success
                StatusMessage = $"Order Successfully Created With Order Number {newOrderResponse?.TheSslStoreOrderId}"
            };
        }


        public override CAConnectorCertificate GetSingleRecord(string caRequestId)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            var orderStatusRequest = _requestManager.GetOrderStatusRequest(caRequestId);
            var certResponse = Task
                .Run(async () => await SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)).Result;

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            return new CAConnectorCertificate
            {
                CARequestID = caRequestId,
                Certificate = string.Empty,
                CSR = string.Empty,
                Status = _requestManager.MapReturnStatus(certResponse?.OrderStatus.MajorStatus)
            };
        }

        [Obsolete]
        public override void Synchronize(ICertificateDataReader certificateDataReader,
            BlockingCollection<CertificateRecord> blockingBuffer,
            CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken,
            string logicalName)
        {
        }

        public override void Synchronize(ICertificateDataReader certificateDataReader,
            BlockingCollection<CAConnectorCertificate> blockingBuffer,
            CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            try
            {
                //Get a list of all the templates need ones for Existing Orgs so we can fill the org dropdown
                var templates=new BlockingCollection<ITemplate>(100);
                KeyfactorClient.SubmitQueryTemplatesRequestAsync(templates, cancelToken, _requestManager);

                //Get a list of orgs from digicert via SslStore API
                var organizationListRequest = _requestManager.GetOrganizationListRequest();
                var orgListResponse = Task
                    .Run(async () => await SslStoreClient.SubmitOrganizationListAsync(organizationListRequest)).Result;
                
                foreach (var template in templates.GetConsumingEnumerable(cancelToken))
                {
                    var currentTemplate = (Template) template;
                    if (cancelToken.IsCancellationRequested)
                    {
                        Logger.Error("Synchronize was canceled Getting Templates");
                        break;
                    }
                    //If it is an existing org template then fill dropdown with existing organizations
                    if(currentTemplate.CommonName.EndsWith("-EO"))
                    {
                        var orgIdField = currentTemplate.EnrollmentFields.Find(e => e.Name == "Organization ID");
                        var currentId = orgIdField.Id;
                        currentTemplate.EnrollmentFields.Remove(orgIdField);
                        var newOrgField = new EnrollmentField();
                        newOrgField.Id = currentId;
                        newOrgField.Name = "Organization ID";
                        newOrgField.DataType = 2;
                        List<string> optionsList = new List<string>();
                        var orgLength = 0;
                        foreach(Organization org in orgListResponse.OrganizationList)
                        {
                            var newOrgName = org.Name + " (" + org.TssOrganizationId + ") " + org.City + "-" + org.Country.ToUpper();
                            orgLength = orgLength + newOrgName.Length +1;
                            if(orgLength<=950 && org.Status=="active") 
                            {
                                optionsList.Add(newOrgName);
                            }
                        }
                        newOrgField.Options.AddRange(optionsList);
                        currentTemplate.EnrollmentFields.Insert(0, newOrgField);

                        var updateOrgResponse = Task.Run(async () => await KeyfactorClient.SubmitUpdateTemplateAsync(currentTemplate)).Result;
                        
                    }
                }

                
                var certs = new BlockingCollection<INewOrderResponse>(100);
                SslStoreClient.SubmitQueryOrderRequestAsync(certs, cancelToken, _requestManager);

                foreach (var currentResponseItem in certs.GetConsumingEnumerable(cancelToken))
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        Logger.Error("Synchronize was canceled.");
                        break;
                    }

                    try
                    {
                        Logger.Trace($"Took Certificate ID {currentResponseItem?.TheSslStoreOrderId} from Queue");

                        //Call GetOrderStatus since this has the most recent status, query order is updated periodically
                        var orderStatusRequest = _requestManager.GetOrderStatusRequest(currentResponseItem?.TheSslStoreOrderId);
                        var orderStatusResponse = Task.Run(async () =>
                            await SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)).Result;

                        var fileContent = "";
                        var certStatus =
                            _requestManager.MapReturnStatus(orderStatusResponse.OrderStatus.MajorStatus);

                        if (certStatus == Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.ISSUED))
                        {
                            var downloadCertificateRequest =
                                _requestManager.GetCertificateRequest(orderStatusResponse.TheSslStoreOrderId);
                            var certResponse =
                                Task.Run(async () =>
                                        await SslStoreClient.SubmitDownloadCertificateAsync(
                                            downloadCertificateRequest))
                                    .Result;
                            if (!certResponse.AuthResponse.IsError)
                            {
                                fileContent = _requestManager.GetCertificateContent(certResponse.Certificates,
                                    orderStatusResponse.CommonName);
                            }
                        }


                        //Keyfactor sync only seems to work when there is a valid cert and I can only get Active valid certs from SSLStore
                        if (certStatus == Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.ISSUED) &&
                            fileContent.Length > 0 || certStatus ==
                            Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.REVOKED))
                        {
                            string serialNumber="";
                            if (fileContent.Length > 0)
                            {
                                var cert = new X509Certificate2(Encoding.UTF8.GetBytes(fileContent));
                                serialNumber = cert.SerialNumber;
                            }

                            blockingBuffer.Add(new CAConnectorCertificate
                            {
                                CARequestID =
                                    $"{orderStatusResponse.TheSslStoreOrderId}-{serialNumber}",
                                Certificate = fileContent,
                                SubmissionDate = Convert.ToDateTime(orderStatusResponse.PurchaseDate),
                                Status = certStatus,
                                ProductID = $"{orderStatusResponse.ProductCode}"
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Error("Synchronize was canceled.");
                        break;
                    }
                }
            }
            catch (AggregateException aggEx)
            {
                Logger.Error("SslStore Synchronize Task failed!");
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                // ReSharper disable once PossibleIntendedRethrow
                throw aggEx;
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }


        public override void Initialize(ICAConnectorConfigProvider configProvider)
        {
            PartnerCode = configProvider.CAConnectionData[Constants.PartnerCode].ToString();
            AuthenticationToken = configProvider.CAConnectionData[Constants.AuthToken].ToString();
            SslStoreClient = new SslStoreClient(configProvider);
            KeyfactorClient=new KeyfactorClient(configProvider);
            PageSize = _requestManager.GetClientPageSize(configProvider);
            ConfigManager = configProvider;
        }

        public override void Ping()
        {
        }

        public override void ValidateCAConnectionInfo(Dictionary<string, object> connectionInfo)
        {
        }

        public override void ValidateProductInfo(EnrollmentProductInfo productInfo,
            Dictionary<string, object> connectionInfo)
        {
        }

        private string ValidateEmails(Task<EmailApproverResponse> validEmails,string[] arrayApproverEmails, EnrollmentProductInfo productInfo,int count)
        {
            if (arrayApproverEmails.Length > 1 && productInfo.ProductID.Contains("digi"))
            {
                return $"There should only be one approval email for Digicert products.";
            }

            //Only validate the first domain for digicert, that is the only one that has to match approver email
            if (count==1 && productInfo.ProductID.Contains("digi") && arrayApproverEmails.Length>0)
            {
                if (!validEmails.Result.ApproverEmailList.Contains(arrayApproverEmails[0]))
                
                {
                    return $"Digicert Approver Email must be one of the following {string.Join(",", validEmails.Result.ApproverEmailList)}";
                }
            }

            if (!productInfo.ProductID.Contains("digi"))
            {
                //See if emails passed in match any of the valid approver emails if not then error                
                if (!validEmails.Result.ApproverEmailList.Intersect(arrayApproverEmails).Any())
                {
                    return $"Sectigo Approver Email must be one of the following {string.Join(",", validEmails.Result.ApproverEmailList)}";
                }

            }

            return "";
        }
    }
}