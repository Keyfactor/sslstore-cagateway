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
using Newtonsoft.Json;

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
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            Logger.Trace($"Entering Revoke Method: caRequestId={caRequestId ?? "(null)"}, hexSerialNumber={hexSerialNumber ?? "(null)"}, revocationReason={revocationReason}");
            try
            {
                if (string.IsNullOrEmpty(caRequestId))
                {
                    Logger.Error("Revoke called with null or empty caRequestId.");
                    return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
                }

                var caRequestIdParts = caRequestId.Split('-');
                Logger.Trace($"Revoke caRequestId split into {caRequestIdParts.Length} parts, using first part: {caRequestIdParts[0]}");

                var revokeOrderRequest = _requestManager.GetRevokeOrderRequest(caRequestIdParts[0]);
                Logger.Trace($"Revoke Request JSON {JsonConvert.SerializeObject(revokeOrderRequest)}");

                var requestResponse =
                    Task.Run(async () => await SslStoreClient.SubmitRevokeCertificateAsync(revokeOrderRequest)).Result;

                Logger.Trace($"Revoke Response JSON {JsonConvert.SerializeObject(requestResponse)}");

                if (requestResponse == null)
                {
                    Logger.Error("Revoke response is null.");
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                    return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
                }

                if (requestResponse.AuthResponse == null)
                {
                    Logger.Error("Revoke response AuthResponse is null.");
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                    return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
                }

                if (requestResponse.AuthResponse.IsError)
                {
                    Logger.Error($"Revoke Error Occurred: {JsonConvert.SerializeObject(requestResponse.AuthResponse)}");
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                    return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
                }

                Logger.Trace("Revoke completed successfully.");
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

                return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.REVOKED);
            }
            catch (Exception e)
            {
                Logger.Error($"An Error has occurred during the revoke process: {e.Message}\n{e.StackTrace}");
                if (e.InnerException != null)
                    Logger.Error($"Inner exception: {e.InnerException.Message}\n{e.InnerException.StackTrace}");
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
            Logger.Trace($"Enroll called: enrollmentType={enrollmentType}, subject={subject ?? "(null)"}, ProductID={productInfo?.ProductID ?? "(null)"}");

            try
            {
                if (productInfo == null)
                {
                    Logger.Error("Enroll called with null productInfo.");
                    return new EnrollmentResult { Status = 30, StatusMessage = "Product info is null." };
                }

                if (productInfo.ProductParameters == null)
                {
                    Logger.Error("Enroll called with null ProductParameters.");
                    return new EnrollmentResult { Status = 30, StatusMessage = "Product parameters are null." };
                }

                Logger.Trace($"Enroll ProductParameters keys: {string.Join(", ", productInfo.ProductParameters.Keys)}");

                INewOrderResponse enrollmentResponse = null;
                NewOrderRequest enrollmentRequest;
                ReIssueRequest reIssueRequest;
                CAConnectorCertificate priorCert;
                OrderStatusRequest orderStatusRequest;
                INewOrderResponse orderStatusResponse;

                switch (enrollmentType)
                {
                    case RequestUtilities.EnrollmentType.New:
                        Logger.Trace("Entering New Enrollment");
                        //If they renewed an expired cert it gets here and this will not be supported
                        if (!productInfo.ProductParameters.ContainsKey("PriorCertSN"))
                        {
                            string[] arrayProducts = Array.Empty<string>();
                            string[] arrayApproverEmails = Array.Empty<string>();

                            if (productInfo.ProductParameters.ContainsKey("DNS Names Comma Separated"))
                            {
                                Logger.Trace($"DNS Comma Separated {productInfo.ProductParameters["DNS Names Comma Separated"]}");
                                arrayProducts = productInfo.ProductParameters["DNS Names Comma Separated"].Split(new char[] { ',' });
                            }
                            if (productInfo.ProductParameters.ContainsKey("Approver Email"))
                            {
                                Logger.Trace($"Approver Email {productInfo.ProductParameters["Approver Email"]}");
                                arrayApproverEmails = productInfo.ProductParameters["Approver Email"].Split(new char[] { ',' });
                            }

                            Logger.Trace($"New Enrollment: {arrayProducts.Length} DNS products, {arrayApproverEmails.Length} approver emails");

                            var count = 1;
                            foreach (var product in arrayProducts)
                            {
                                Logger.Trace($"Validating email approver for product domain '{product}' (#{count})...");
                                var emailApproverRequest = _requestManager.GetEmailApproverListRequest(productInfo.ProductID, product);
                                Logger.Trace($"Email Approver Request JSON {JsonConvert.SerializeObject(emailApproverRequest)}");

                                var emailApproverResponse = Task.Run(async () =>
                                    await SslStoreClient.SubmitEmailApproverRequestAsync(emailApproverRequest));

                                Logger.Trace($"Email Approver Response JSON {JsonConvert.SerializeObject(emailApproverResponse)}");

                                var emailValidation = ValidateEmails(emailApproverResponse, arrayApproverEmails, productInfo, count);

                                Logger.Trace($"Email Validation Result {emailValidation}");

                                if (emailValidation.Length > 0)
                                {
                                    Logger.Warn($"Email validation failed: {emailValidation}");
                                    return new EnrollmentResult
                                    {
                                        Status = 30, //failure
                                        StatusMessage = emailValidation
                                    };
                                }
                                count++;
                            }


                            Logger.Trace("Building enrollment request...");
                            enrollmentRequest =
                                _requestManager.GetEnrollmentRequest(csr, productInfo, ConfigManager, false);

                            Logger.Trace($"enrollmentRequest JSON {JsonConvert.SerializeObject(enrollmentRequest)}");

                            Logger.Trace("Submitting new order request...");
                            enrollmentResponse =
                                Task.Run(async () => await SslStoreClient.SubmitNewOrderRequestAsync(enrollmentRequest))
                                    .Result;

                            Logger.Trace($"enrollmentResponse JSON {JsonConvert.SerializeObject(enrollmentResponse)}");
                        }
                        else
                        {
                            Logger.Warn("Enrollment rejected: PriorCertSN present in New enrollment (expired cert renewal not supported).");
                            return new EnrollmentResult
                            {
                                Status = 30, //failure
                                StatusMessage = "You cannot renew and expired cert please perform an new enrollment."
                            };
                        }

                        break;
                    case RequestUtilities.EnrollmentType.Renew:
                        Logger.Trace("Entering Renew Enrollment");

                        if (!productInfo.ProductParameters.ContainsKey("PriorCertSN"))
                        {
                            Logger.Error("Renew enrollment missing required PriorCertSN parameter.");
                            return new EnrollmentResult { Status = 30, StatusMessage = "PriorCertSN is required for renewal." };
                        }

                        Logger.Trace($"PriorCertSN={productInfo.ProductParameters["PriorCertSN"]}");
                        priorCert = certificateDataReader.GetCertificateRecord(
                            DataConversion.HexToBytes(productInfo.ProductParameters["PriorCertSN"]));

                        if (priorCert == null)
                        {
                            Logger.Error($"Prior certificate not found for PriorCertSN={productInfo.ProductParameters["PriorCertSN"]}");
                            return new EnrollmentResult { Status = 30, StatusMessage = "Prior certificate record not found." };
                        }

                        Logger.Trace($"Prior Cert CARequestID={priorCert.CARequestID ?? "(null)"}");

                        if (string.IsNullOrEmpty(priorCert.CARequestID))
                        {
                            Logger.Error("Prior certificate has null/empty CARequestID.");
                            return new EnrollmentResult { Status = 30, StatusMessage = "Prior certificate has no CA Request ID." };
                        }

                        orderStatusRequest = _requestManager.GetOrderStatusRequest(priorCert.CARequestID.Split('-')[0]);

                        Logger.Trace($"orderStatusRequest JSON {JsonConvert.SerializeObject(orderStatusRequest)}");

                        orderStatusResponse = Task.Run(async () =>
                            await SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)).Result;

                        if (orderStatusResponse == null)
                        {
                            Logger.Error("Order status response is null during renewal.");
                            return new EnrollmentResult { Status = 30, StatusMessage = "Failed to retrieve order status for renewal." };
                        }

                        Logger.Trace($"orderStatusResponse JSON {JsonConvert.SerializeObject(orderStatusResponse)}");

                        var renewRequest = _requestManager.GetRenewalRequest(orderStatusResponse, csr);

                        Logger.Trace($"renewRequest JSON {JsonConvert.SerializeObject(renewRequest)}");

                        Logger.Trace("Submitting renewal request...");
                        enrollmentResponse =
                            Task.Run(async () => await SslStoreClient.SubmitRenewRequestAsync(renewRequest)).Result;

                        Logger.Trace($"enrollmentResponse JSON {JsonConvert.SerializeObject(enrollmentResponse)}");

                        break;
                    case RequestUtilities.EnrollmentType.Reissue:
                        Logger.Trace("Entering Reissue Enrollment");

                        if (!productInfo.ProductParameters.ContainsKey("PriorCertSN"))
                        {
                            Logger.Error("Reissue enrollment missing required PriorCertSN parameter.");
                            return new EnrollmentResult { Status = 30, StatusMessage = "PriorCertSN is required for reissue." };
                        }

                        Logger.Trace($"PriorCertSN={productInfo.ProductParameters["PriorCertSN"]}");
                        priorCert =
                            certificateDataReader.GetCertificateRecord(
                                DataConversion.HexToBytes(productInfo.ProductParameters["PriorCertSN"]));

                        if (priorCert == null)
                        {
                            Logger.Error($"Prior certificate not found for PriorCertSN={productInfo.ProductParameters["PriorCertSN"]}");
                            return new EnrollmentResult { Status = 30, StatusMessage = "Prior certificate record not found." };
                        }

                        Logger.Trace($"Prior Cert CARequestID={priorCert.CARequestID ?? "(null)"}");

                        if (string.IsNullOrEmpty(priorCert.CARequestID))
                        {
                            Logger.Error("Prior certificate has null/empty CARequestID.");
                            return new EnrollmentResult { Status = 30, StatusMessage = "Prior certificate has no CA Request ID." };
                        }

                        orderStatusRequest = _requestManager.GetOrderStatusRequest(priorCert.CARequestID.Split('-')[0]);

                        Logger.Trace($"orderStatusRequest JSON {JsonConvert.SerializeObject(orderStatusRequest)}");

                        orderStatusResponse = Task.Run(async () =>
                            await SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)).Result;

                        if (orderStatusResponse == null)
                        {
                            Logger.Error("Order status response is null during reissue.");
                            return new EnrollmentResult { Status = 30, StatusMessage = "Failed to retrieve order status for reissue." };
                        }

                        Logger.Trace($"orderStatusResponse JSON {JsonConvert.SerializeObject(orderStatusResponse)}");

                        reIssueRequest = _requestManager.GetReIssueRequest(orderStatusResponse, csr, false);

                        Logger.Trace($"reIssueRequest JSON {JsonConvert.SerializeObject(reIssueRequest)}");

                        Logger.Trace("Submitting reissue request...");
                        enrollmentResponse =
                            Task.Run(async () => await SslStoreClient.SubmitReIssueRequestAsync(reIssueRequest)).Result;

                        Logger.Trace($"enrollmentResponse JSON {JsonConvert.SerializeObject(enrollmentResponse)}");

                        break;
                    default:
                        Logger.Error($"Unknown enrollment type: {enrollmentType}");
                        return new EnrollmentResult { Status = 30, StatusMessage = $"Unknown enrollment type: {enrollmentType}" };
                }

                return GetEnrollmentResult(enrollmentResponse);
            }
            catch (Exception ex)
            {
                Logger.Error($"Enroll failed with exception: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                return new EnrollmentResult { Status = 30, StatusMessage = $"Enrollment failed: {ex.Message}" };
            }
        }

        private EnrollmentResult GetEnrollmentResult(INewOrderResponse newOrderResponse)
        {
            Logger.Trace("GetEnrollmentResult called.");

            if (newOrderResponse == null)
            {
                Logger.Error("GetEnrollmentResult: newOrderResponse is null.");
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                return new EnrollmentResult
                {
                    Status = 30,
                    StatusMessage = "Enrollment response was null."
                };
            }

            Logger.Trace($"GetEnrollmentResult: TheSslStoreOrderId={newOrderResponse.TheSslStoreOrderId ?? "(null)"}, AuthResponse is null={newOrderResponse.AuthResponse == null}");

            if (newOrderResponse.AuthResponse != null && newOrderResponse.AuthResponse.IsError)
            {
                var errorMessage = "(no error message)";
                if (newOrderResponse.AuthResponse.Message != null && newOrderResponse.AuthResponse.Message.Count > 0)
                {
                    errorMessage = newOrderResponse.AuthResponse.Message[0];
                }
                Logger.Error($"GetEnrollmentResult: Auth error: {errorMessage}");
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                return new EnrollmentResult
                {
                    Status = 30, //failure
                    StatusMessage = errorMessage
                };
            }

            Logger.Trace($"GetEnrollmentResult: Success, OrderId={newOrderResponse.TheSslStoreOrderId ?? "(null)"}");
            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            return new EnrollmentResult
            {
                Status = 9, //success
                StatusMessage = $"Order Successfully Created With Order Number {newOrderResponse.TheSslStoreOrderId}"
            };
        }


        public override CAConnectorCertificate GetSingleRecord(string caRequestId)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            Logger.Trace($"GetSingleRecord called: caRequestId={caRequestId ?? "(null)"}");

            try
            {
                if (string.IsNullOrEmpty(caRequestId))
                {
                    Logger.Error("GetSingleRecord called with null/empty caRequestId.");
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                    return new CAConnectorCertificate
                    {
                        CARequestID = caRequestId,
                        Certificate = string.Empty,
                        CSR = string.Empty,
                        Status = Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED)
                    };
                }

                var orderStatusRequest = _requestManager.GetOrderStatusRequest(caRequestId);

                Logger.Trace($"orderStatusRequest JSON {JsonConvert.SerializeObject(orderStatusRequest)}");

                var certResponse = Task
                    .Run(async () => await SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)).Result;

                Logger.Trace($"certResponse JSON {JsonConvert.SerializeObject(certResponse)}");

                var majorStatus = certResponse?.OrderStatus?.MajorStatus;
                Logger.Trace($"GetSingleRecord: MajorStatus={majorStatus ?? "(null)"}");

                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                return new CAConnectorCertificate
                {
                    CARequestID = caRequestId,
                    Certificate = string.Empty,
                    CSR = string.Empty,
                    Status = _requestManager.MapReturnStatus(majorStatus)
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"GetSingleRecord failed for caRequestId={caRequestId}: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                return new CAConnectorCertificate
                {
                    CARequestID = caRequestId,
                    Certificate = string.Empty,
                    CSR = string.Empty,
                    Status = Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED)
                };
            }
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
            Logger.Trace("Synchronize starting...");

            try
            {
                //Get a list of all the templates need ones for Existing Orgs so we can fill the org dropdown
                Logger.Trace("Creating templates blocking collection...");
                var templates=new BlockingCollection<ITemplate>(100);
                KeyfactorClient.SubmitQueryTemplatesRequestAsync(templates, cancelToken, _requestManager);
                Logger.Trace("Template query submitted.");

                //Get a list of orgs from digicert via SslStore API
                Logger.Trace("Getting organization list request...");
                var organizationListRequest = _requestManager.GetOrganizationListRequest();

                Logger.Trace($"organizationListRequest JSON {JsonConvert.SerializeObject(organizationListRequest)}");

                Logger.Trace("Submitting organization list request to SslStore...");
                var orgListResponse = Task
                    .Run(async () => await SslStoreClient.SubmitOrganizationListAsync(organizationListRequest)).Result;

                Logger.Trace($"orgListResponse JSON {JsonConvert.SerializeObject(orgListResponse)}");

                if (orgListResponse == null)
                {
                    Logger.Warn("Organization list response is null. Skipping template organization updates.");
                }

                Logger.Trace("Beginning template enumeration...");
                foreach (var template in templates.GetConsumingEnumerable(cancelToken))
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        Logger.Error("Synchronize was canceled Getting Templates");
                        break;
                    }

                    try
                    {
                        if (template == null)
                        {
                            Logger.Warn("Encountered null template in collection, skipping.");
                            continue;
                        }

                        var currentTemplate = (Template) template;
                        Logger.Trace($"Processing template Id={currentTemplate.Id}, CommonName={currentTemplate.CommonName ?? "(null)"}");

                        //If it is an existing org template then fill dropdown with existing organizations
                        if (currentTemplate.CommonName != null && currentTemplate.CommonName.EndsWith("-EO"))
                        {
                            Logger.Trace($"Ends in -EO Common Name {currentTemplate.CommonName}");

                            if (currentTemplate.EnrollmentFields == null)
                            {
                                Logger.Warn($"Template {currentTemplate.CommonName} has null EnrollmentFields, skipping org update.");
                                continue;
                            }

                            var orgIdField = currentTemplate.EnrollmentFields.Find(e => e.Name == "Organization ID");
                            if (orgIdField == null)
                            {
                                Logger.Warn($"Template {currentTemplate.CommonName} has no 'Organization ID' enrollment field, skipping org update.");
                                continue;
                            }

                            var currentId = orgIdField.Id;
                            currentTemplate.EnrollmentFields.Remove(orgIdField);
                            var newOrgField = new EnrollmentField();
                            newOrgField.Id = currentId;
                            newOrgField.Name = "Organization ID";
                            newOrgField.DataType = 2;
                            List<string> optionsList = new List<string>();
                            var orgLength = 0;

                            if (orgListResponse?.OrganizationList != null)
                            {
                                Logger.Trace($"Processing {orgListResponse.OrganizationList.Count} organizations for template {currentTemplate.CommonName}");
                                foreach (Organization org in orgListResponse.OrganizationList)
                                {
                                    if (org == null)
                                    {
                                        Logger.Warn("Encountered null organization in list, skipping.");
                                        continue;
                                    }

                                    var orgName = org.Name ?? "(no name)";
                                    var orgCity = org.City ?? "(no city)";
                                    var orgCountry = org.Country ?? "(no country)";
                                    var newOrgName = orgName + " (" + org.TssOrganizationId + ") " + orgCity + "-" + orgCountry.ToUpper();
                                    orgLength = orgLength + newOrgName.Length + 1;
                                    if (orgLength <= 950 && org.Status == "active")
                                    {
                                        optionsList.Add(newOrgName);
                                    }
                                }
                            }
                            else
                            {
                                Logger.Warn("Organization list is null or empty, no orgs will be added to template dropdown.");
                            }

                            Logger.Trace($"Adding {optionsList.Count} organizations to template dropdown for {currentTemplate.CommonName}");
                            newOrgField.Options.AddRange(optionsList);
                            currentTemplate.EnrollmentFields.Insert(0, newOrgField);

                            Logger.Trace($"Submitting template update for {currentTemplate.CommonName}...");
                            var updateOrgResponse = Task.Run(async () => await KeyfactorClient.SubmitUpdateTemplateAsync(currentTemplate)).Result;
                            Logger.Trace($"Template update response for {currentTemplate.CommonName}: {JsonConvert.SerializeObject(updateOrgResponse)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error processing template during sync: {ex.Message}\n{ex.StackTrace}");
                        if (ex.InnerException != null)
                            Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                    }
                }

                Logger.Trace("Template processing complete. Beginning certificate sync...");
                var certs = new BlockingCollection<INewOrderResponse>(100);
                SslStoreClient.SubmitQueryOrderRequestAsync(certs, cancelToken, _requestManager);
                Logger.Trace("Certificate query submitted.");

                var certCount = 0;
                var errorCount = 0;
                var addedCount = 0;

                foreach (var currentResponseItem in certs.GetConsumingEnumerable(cancelToken))
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        Logger.Error("Synchronize was canceled.");
                        break;
                    }

                    certCount++;
                    try
                    {
                        if (currentResponseItem == null)
                        {
                            Logger.Warn("Encountered null certificate response item in queue, skipping.");
                            continue;
                        }

                        var orderId = currentResponseItem.TheSslStoreOrderId ?? "(null)";
                        Logger.Trace($"Took Certificate ID {orderId} from Queue (item #{certCount})");

                        if (string.IsNullOrEmpty(currentResponseItem.TheSslStoreOrderId))
                        {
                            Logger.Warn($"Certificate response item #{certCount} has null/empty TheSslStoreOrderId, skipping.");
                            continue;
                        }

                        //Call GetOrderStatus since this has the most recent status, query order is updated periodically
                        Logger.Trace($"Getting order status for order {orderId}...");
                        var orderStatusRequest = _requestManager.GetOrderStatusRequest(currentResponseItem.TheSslStoreOrderId);
                        var orderStatusResponse = Task.Run(async () =>
                            await SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)).Result;

                        if (orderStatusResponse == null)
                        {
                            Logger.Warn($"Order status response is null for order {orderId}, skipping.");
                            continue;
                        }

                        Logger.Trace($"Order status response for {orderId}: OrderStatus={JsonConvert.SerializeObject(orderStatusResponse.OrderStatus)}, CommonName={orderStatusResponse.CommonName ?? "(null)"}, ProductCode={orderStatusResponse.ProductCode ?? "(null)"}");

                        if (orderStatusResponse.OrderStatus == null)
                        {
                            Logger.Warn($"OrderStatus object is null for order {orderId}, skipping.");
                            continue;
                        }

                        Logger.Trace($"Order {orderId} MajorStatus={orderStatusResponse.OrderStatus.MajorStatus ?? "(null)"}, MinorStatus={orderStatusResponse.OrderStatus.MinorStatus ?? "(null)"}");

                        var fileContent = "";
                        var certStatus =
                            _requestManager.MapReturnStatus(orderStatusResponse.OrderStatus.MajorStatus);
                        Logger.Trace($"Order {orderId} mapped certStatus={certStatus}");

                        if (certStatus == Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.ISSUED))
                        {
                            Logger.Trace($"Order {orderId} is ISSUED, downloading certificate...");
                            try
                            {
                                var downloadCertificateRequest =
                                    _requestManager.GetCertificateRequest(orderStatusResponse.TheSslStoreOrderId);
                                var certResponse =
                                    Task.Run(async () =>
                                            await SslStoreClient.SubmitDownloadCertificateAsync(
                                                downloadCertificateRequest))
                                        .Result;

                                if (certResponse == null)
                                {
                                    Logger.Warn($"Certificate download response is null for order {orderId}.");
                                }
                                else if (certResponse.AuthResponse == null)
                                {
                                    Logger.Warn($"Certificate download AuthResponse is null for order {orderId}.");
                                }
                                else if (!certResponse.AuthResponse.IsError)
                                {
                                    Logger.Trace($"Certificate downloaded successfully for order {orderId}, extracting content...");
                                    fileContent = _requestManager.GetCertificateContent(certResponse.Certificates,
                                        orderStatusResponse.CommonName) ?? "";
                                    Logger.Trace($"Order {orderId} certificate content length={fileContent.Length}");
                                }
                                else
                                {
                                    Logger.Warn($"Certificate download returned error for order {orderId}: {JsonConvert.SerializeObject(certResponse.AuthResponse)}");
                                }
                            }
                            catch (Exception dlEx)
                            {
                                Logger.Error($"Error downloading certificate for order {orderId}: {dlEx.Message}\n{dlEx.StackTrace}");
                                if (dlEx.InnerException != null)
                                    Logger.Error($"Inner exception: {dlEx.InnerException.Message}\n{dlEx.InnerException.StackTrace}");
                            }
                        }


                        //Keyfactor sync only seems to work when there is a valid cert and I can only get Active valid certs from SSLStore
                        if ((certStatus == Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.ISSUED) &&
                            fileContent.Length > 0) || certStatus ==
                            Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.REVOKED))
                        {
                            string serialNumber = "";
                            if (fileContent.Length > 0)
                            {
                                try
                                {
                                    var cert = new X509Certificate2(Encoding.UTF8.GetBytes(fileContent));
                                    serialNumber = cert.SerialNumber ?? "";
                                    Logger.Trace($"Order {orderId} parsed certificate serial={serialNumber}");
                                }
                                catch (Exception certParseEx)
                                {
                                    Logger.Error($"Error parsing X509 certificate for order {orderId}: {certParseEx.Message}\n{certParseEx.StackTrace}");
                                    continue;
                                }
                            }

                            Logger.Trace($"Adding order {orderId} to sync buffer: CARequestID={orderStatusResponse.TheSslStoreOrderId}-{serialNumber}, Status={certStatus}, ProductID={orderStatusResponse.ProductCode ?? "(null)"}, PurchaseDate={orderStatusResponse.PurchaseDate ?? "(null)"}");

                            blockingBuffer.Add(new CAConnectorCertificate
                            {
                                CARequestID =
                                    $"{orderStatusResponse.TheSslStoreOrderId}-{serialNumber}",
                                Certificate = fileContent,
                                SubmissionDate = string.IsNullOrEmpty(orderStatusResponse.PurchaseDate)
                                    ? DateTime.UtcNow
                                    : Convert.ToDateTime(orderStatusResponse.PurchaseDate),
                                Status = certStatus,
                                ProductID = $"{orderStatusResponse.ProductCode}"
                            });
                            addedCount++;
                        }
                        else
                        {
                            Logger.Trace($"Order {orderId} not added to sync buffer: certStatus={certStatus}, fileContentLength={fileContent.Length}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Error("Synchronize was canceled.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        Logger.Error($"Error processing certificate item #{certCount} during sync: {ex.Message}\n{ex.StackTrace}");
                        if (ex.InnerException != null)
                            Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                    }
                }

                Logger.Trace($"Synchronize certificate processing complete. Total processed={certCount}, Added to buffer={addedCount}, Errors={errorCount}");
            }
            catch (AggregateException aggEx)
            {
                var flatEx = aggEx.Flatten();
                Logger.Error($"SslStore Synchronize Task failed! {flatEx.Message}");
                foreach (var innerEx in flatEx.InnerExceptions)
                {
                    Logger.Error($"  Inner exception: {innerEx.Message}\n{innerEx.StackTrace}");
                }
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"SslStore Synchronize unexpected error: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                throw;
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }


        public override void Initialize(ICAConnectorConfigProvider configProvider)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            Logger.Trace("Initialize called.");

            try
            {
                if (configProvider == null)
                {
                    Logger.Error("Initialize called with null configProvider.");
                    throw new ArgumentNullException(nameof(configProvider));
                }

                if (configProvider.CAConnectionData == null)
                {
                    Logger.Error("Initialize: CAConnectionData is null.");
                    throw new InvalidOperationException("CAConnectionData is null.");
                }

                Logger.Trace($"Initialize: CAConnectionData keys: {string.Join(", ", configProvider.CAConnectionData.Keys)}");

                if (!configProvider.CAConnectionData.ContainsKey(Constants.PartnerCode))
                {
                    Logger.Error($"Initialize: Missing required config key '{Constants.PartnerCode}'.");
                    throw new InvalidOperationException($"Missing required config key '{Constants.PartnerCode}'.");
                }

                if (!configProvider.CAConnectionData.ContainsKey(Constants.AuthToken))
                {
                    Logger.Error($"Initialize: Missing required config key '{Constants.AuthToken}'.");
                    throw new InvalidOperationException($"Missing required config key '{Constants.AuthToken}'.");
                }

                PartnerCode = configProvider.CAConnectionData[Constants.PartnerCode]?.ToString();
                Logger.Trace($"Initialize: PartnerCode={PartnerCode ?? "(null)"}");

                AuthenticationToken = configProvider.CAConnectionData[Constants.AuthToken]?.ToString();
                Logger.Trace("Initialize: AuthenticationToken set (value masked).");

                Logger.Trace("Initialize: Creating SslStoreClient...");
                SslStoreClient = new SslStoreClient(configProvider);

                Logger.Trace("Initialize: Creating KeyfactorClient...");
                KeyfactorClient = new KeyfactorClient(configProvider);

                Logger.Trace("Initialize: Getting page size...");
                PageSize = _requestManager.GetClientPageSize(configProvider);
                Logger.Trace($"Initialize: PageSize={PageSize}");

                ConfigManager = configProvider;

                Logger.Trace("Initialize complete.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Initialize failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
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

        private string ValidateEmails(Task<EmailApproverResponse> validEmails, string[] arrayApproverEmails, EnrollmentProductInfo productInfo, int count)
        {
            Logger.Trace($"ValidateEmails called: count={count}, approverEmailCount={arrayApproverEmails?.Length ?? 0}, ProductID={productInfo?.ProductID ?? "(null)"}");

            try
            {
                if (validEmails == null)
                {
                    Logger.Error("ValidateEmails: validEmails task is null.");
                    return "Email approver validation failed: no response from approver list request.";
                }

                if (arrayApproverEmails == null)
                {
                    Logger.Error("ValidateEmails: arrayApproverEmails is null.");
                    return "Email approver validation failed: approver email list is null.";
                }

                if (productInfo?.ProductID == null)
                {
                    Logger.Error("ValidateEmails: productInfo or ProductID is null.");
                    return "Email approver validation failed: product info is null.";
                }

                if (arrayApproverEmails.Length > 1 && productInfo.ProductID.Contains("digi"))
                {
                    Logger.Warn("ValidateEmails: Multiple approver emails for Digicert product.");
                    return "There should only be one approval email for Digicert products.";
                }

                // Wait for the task and check for errors
                EmailApproverResponse emailResult;
                try
                {
                    emailResult = validEmails.Result;
                }
                catch (AggregateException aggEx)
                {
                    var flatEx = aggEx.Flatten();
                    Logger.Error($"ValidateEmails: Failed to get email approver list: {flatEx.Message}\n{flatEx.StackTrace}");
                    return $"Failed to retrieve email approver list: {flatEx.Message}";
                }

                if (emailResult == null)
                {
                    Logger.Error("ValidateEmails: Email approver response result is null.");
                    return "Email approver validation failed: response was null.";
                }

                if (emailResult.ApproverEmailList == null)
                {
                    Logger.Error("ValidateEmails: ApproverEmailList is null in response.");
                    return "Email approver validation failed: approver email list in response was null.";
                }

                Logger.Trace($"ValidateEmails: Valid approver emails from API: {string.Join(", ", emailResult.ApproverEmailList)}");

                //Only validate the first domain for digicert, that is the only one that has to match approver email
                if (count == 1 && productInfo.ProductID.Contains("digi") && arrayApproverEmails.Length > 0)
                {
                    Logger.Trace($"ValidateEmails: Checking Digicert approver email '{arrayApproverEmails[0]}' against valid list.");
                    if (!emailResult.ApproverEmailList.Contains(arrayApproverEmails[0]))
                    {
                        return $"Digicert Approver Email must be one of the following {string.Join(",", emailResult.ApproverEmailList)}";
                    }
                }

                if (!productInfo.ProductID.Contains("digi"))
                {
                    Logger.Trace("ValidateEmails: Checking Sectigo approver emails against valid list.");
                    //See if emails passed in match any of the valid approver emails if not then error
                    if (!emailResult.ApproverEmailList.Intersect(arrayApproverEmails).Any())
                    {
                        return $"Sectigo Approver Email must be one of the following {string.Join(",", emailResult.ApproverEmailList)}";
                    }
                }

                Logger.Trace("ValidateEmails: Validation passed.");
                return "";
            }
            catch (Exception ex)
            {
                Logger.Error($"ValidateEmails unexpected error: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                    Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                return $"Email validation failed with unexpected error: {ex.Message}";
            }
        }
    }
}