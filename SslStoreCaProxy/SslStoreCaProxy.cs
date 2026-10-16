using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
        private const int FailedStatus = 30;

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
        private int RenewalWindowDays { get; set; } = Constants.DefaultRenewalWindowDays;

        private FlowLogger NewFlow(string name)
        {
            return new FlowLogger(message => Logger.Trace(message), name);
        }

        private static T RunSync<T>(Func<Task<T>> operation)
        {
            //GetAwaiter().GetResult() surfaces the real exception instead of an AggregateException
            return Task.Run(operation).GetAwaiter().GetResult();
        }

        private static EnrollmentResult Failure(string message)
        {
            return new EnrollmentResult
            {
                Status = FailedStatus,
                StatusMessage = message
            };
        }

        public override int Revoke(string caRequestId, string hexSerialNumber, uint revocationReason)
        {
            using (var flow = NewFlow("Revoke"))
            {
                return RevokeInternal(flow, caRequestId, hexSerialNumber, revocationReason);
            }
        }

        private int RevokeInternal(FlowLogger flow, string caRequestId, string hexSerialNumber, uint revocationReason)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            Logger.Trace($"Entering Revoke Method: caRequestId={caRequestId ?? "(null)"}, hexSerialNumber={hexSerialNumber ?? "(null)"}, revocationReason={revocationReason}");
            try
            {
                if (string.IsNullOrEmpty(caRequestId))
                {
                    Logger.Error("Revoke called with null or empty caRequestId.");
                    flow.Fail("ValidateInput", "caRequestId is null or empty");
                    return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
                }

                var caRequestIdParts = caRequestId.Split('-');
                Logger.Trace($"Revoke caRequestId split into {caRequestIdParts.Length} parts, using first part: {caRequestIdParts[0]}");

                flow.Step("ValidateInput", $"order={caRequestIdParts[0]}, reason={revocationReason}");
                var revokeOrderRequest = _requestManager.GetRevokeOrderRequest(caRequestIdParts[0]);
                Logger.Trace($"Revoke Request JSON {LogRedactor.Json(revokeOrderRequest)}");

                var requestResponse = flow.Step("SubmitRevokeToSslStore",
                    () => RunSync(() => SslStoreClient.SubmitRevokeCertificateAsync(revokeOrderRequest)));

                Logger.Trace($"Revoke Response JSON {LogRedactor.Json(requestResponse)}");

                if (requestResponse == null)
                {
                    Logger.Error("Revoke response is null.");
                    flow.Fail("EvaluateResponse", "response is null");
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                    return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
                }

                if (requestResponse.AuthResponse == null)
                {
                    Logger.Error("Revoke response AuthResponse is null.");
                    flow.Fail("EvaluateResponse", "AuthResponse is null");
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                    return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
                }

                if (requestResponse.AuthResponse.IsError)
                {
                    Logger.Error($"Revoke Error Occurred: {LogRedactor.Json(requestResponse.AuthResponse)}");
                    flow.Fail("EvaluateResponse", "SSL Store returned an error");
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                    return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
                }

                flow.Step("EvaluateResponse", "revoked");
                Logger.Trace("Revoke completed successfully.");
                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

                return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.REVOKED);
            }
            catch (Exception e)
            {
                flow.Fail("Revoke", e.Message);
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

            using (var flow = NewFlow($"Enroll-{enrollmentType}"))
            {
                try
                {
                    return EnrollInternal(flow, certificateDataReader, csr, productInfo, enrollmentType);
                }
                catch (Exception ex)
                {
                    flow.Fail("Enroll", ex.Message);
                    Logger.Error($"Enroll failed with exception: {ex.Message}\n{ex.StackTrace}");
                    if (ex.InnerException != null)
                        Logger.Error($"Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                    return Failure($"Enrollment failed: {ex.Message}");
                }
                finally
                {
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                }
            }
        }

        private EnrollmentResult EnrollInternal(FlowLogger flow, ICertificateDataReader certificateDataReader,
            string csr, EnrollmentProductInfo productInfo, RequestUtilities.EnrollmentType requestedType)
        {
            if (productInfo == null)
            {
                flow.Fail("ValidateInput", "productInfo is null");
                return Failure("Product info is null.");
            }

            if (productInfo.ProductParameters == null)
            {
                flow.Fail("ValidateInput", "ProductParameters is null");
                return Failure("Product parameters are null.");
            }

            var hasPriorCert = productInfo.ProductParameters.TryGetValue("PriorCertSN", out var priorCertSn) &&
                               !string.IsNullOrWhiteSpace(priorCertSn);
            flow.Step("ValidateInput", $"product={productInfo.ProductID}, hasPriorCert={hasPriorCert}");
            Logger.Trace($"Enroll ProductParameters keys: {string.Join(", ", productInfo.ProductParameters.Keys)}");

            if (requestedType != RequestUtilities.EnrollmentType.New &&
                requestedType != RequestUtilities.EnrollmentType.Renew &&
                requestedType != RequestUtilities.EnrollmentType.Reissue)
            {
                flow.Fail("SelectPath", $"unsupported enrollment type {requestedType}");
                return Failure($"Unknown enrollment type: {requestedType}");
            }

            if (!hasPriorCert && requestedType != RequestUtilities.EnrollmentType.New)
            {
                flow.Fail("ValidateInput", $"{requestedType} requires PriorCertSN");
                return Failure($"PriorCertSN is required for {requestedType.ToString().ToLowerInvariant()}.");
            }

            // New, Renew and Reissue requests that carry a prior certificate are resolved from the prior certificate's
            // order, not from what Command asked for, so shorter-lived (~200 day) certs under an active order are
            // reissued instead of renewed (and an expired order is simply ordered again).
            INewOrderResponse priorOrder = null;
            var enrollmentType = requestedType;
            if (hasPriorCert)
            {
                priorOrder = GetPriorOrder(flow, certificateDataReader, priorCertSn);
                enrollmentType = ResolveEnrollmentType(flow, priorOrder, requestedType);
            }
            else
            {
                flow.Skip("EvaluateOrderValidity", "no PriorCertSN");
            }

            flow.Step("EnrollmentDecision", $"requested={requestedType}, resolved={enrollmentType}");

            INewOrderResponse enrollmentResponse;
            switch (enrollmentType)
            {
                case RequestUtilities.EnrollmentType.New:
                    if (hasPriorCert)
                        flow.Step("PriorCertNotReused", "ordering as a new certificate");
                    var newOrderFailure = SubmitNewOrder(flow, csr, productInfo, out enrollmentResponse);
                    if (newOrderFailure != null) return newOrderFailure;
                    break;
                case RequestUtilities.EnrollmentType.Renew:
                    var renewRequest = flow.Step("BuildRenewalRequest",
                        () => _requestManager.GetRenewalRequest(priorOrder, csr));
                    Logger.Trace($"renewRequest JSON {LogRedactor.Json(renewRequest)}");
                    enrollmentResponse = flow.Step("SubmitRenewalToSslStore",
                        () => RunSync(() => SslStoreClient.SubmitRenewRequestAsync(renewRequest)));
                    break;
                default:
                    var reIssueRequest = flow.Step("BuildReissueRequest",
                        () => _requestManager.GetReIssueRequest(priorOrder, csr, false));
                    Logger.Trace($"reIssueRequest JSON {LogRedactor.Json(reIssueRequest)}");
                    enrollmentResponse = flow.Step("SubmitReissueToSslStore",
                        () => RunSync(() => SslStoreClient.SubmitReIssueRequestAsync(reIssueRequest)));
                    break;
            }

            Logger.Trace($"enrollmentResponse JSON {LogRedactor.Json(enrollmentResponse)}");

            var result = GetEnrollmentResult(enrollmentResponse);
            if (result.Status == FailedStatus)
                flow.Fail("MapEnrollmentResult", $"SSL Store rejected the request: {result.StatusMessage}");
            else
                flow.Step("MapEnrollmentResult", $"status={result.Status}");
            return result;
        }

        /// <summary>
        ///     Validates approver emails and submits a brand new order. Returns a failure result, or null on success.
        /// </summary>
        private EnrollmentResult SubmitNewOrder(FlowLogger flow, string csr, EnrollmentProductInfo productInfo,
            out INewOrderResponse enrollmentResponse)
        {
            enrollmentResponse = null;
            var arrayProducts = Array.Empty<string>();
            var arrayApproverEmails = Array.Empty<string>();

            if (productInfo.ProductParameters.ContainsKey("DNS Names Comma Separated"))
            {
                Logger.Trace($"DNS Comma Separated {productInfo.ProductParameters["DNS Names Comma Separated"]}");
                arrayProducts = productInfo.ProductParameters["DNS Names Comma Separated"].Split(',');
            }

            if (productInfo.ProductParameters.ContainsKey("Approver Email"))
            {
                Logger.Trace($"Approver Email {productInfo.ProductParameters["Approver Email"]}");
                arrayApproverEmails = productInfo.ProductParameters["Approver Email"].Split(',');
            }

            Logger.Trace($"New Enrollment: {arrayProducts.Length} DNS products, {arrayApproverEmails.Length} approver emails");

            var count = 1;
            foreach (var product in arrayProducts)
            {
                Logger.Trace($"Validating email approver for product domain '{product}' (#{count})...");
                var emailApproverRequest = _requestManager.GetEmailApproverListRequest(productInfo.ProductID, product);
                Logger.Trace($"Email Approver Request JSON {LogRedactor.Json(emailApproverRequest)}");

                var emailApproverResponse = Task.Run(async () =>
                    await SslStoreClient.SubmitEmailApproverRequestAsync(emailApproverRequest));

                var emailValidation = ValidateEmails(emailApproverResponse, arrayApproverEmails, productInfo, count);
                Logger.Trace($"Email Validation Result {emailValidation}");

                if (emailValidation.Length > 0)
                {
                    Logger.Warn($"Email validation failed: {emailValidation}");
                    flow.Fail("ValidateApproverEmails", emailValidation);
                    return Failure(emailValidation);
                }

                count++;
            }

            flow.Step("ValidateApproverEmails", $"{arrayProducts.Length} domain(s)");

            var enrollmentRequest = flow.Step("BuildNewOrderRequest",
                () => _requestManager.GetEnrollmentRequest(csr, productInfo, ConfigManager, false));
            Logger.Trace($"enrollmentRequest JSON {LogRedactor.Json(enrollmentRequest)}");

            enrollmentResponse = flow.Step("SubmitNewOrderToSslStore",
                () => RunSync(() => SslStoreClient.SubmitNewOrderRequestAsync(enrollmentRequest)));
            return null;
        }

        /// <summary>
        ///     Finds the prior certificate in the gateway database and returns the current status of its SSL Store order.
        /// </summary>
        private INewOrderResponse GetPriorOrder(FlowLogger flow, ICertificateDataReader certificateDataReader,
            string priorCertSn)
        {
            Logger.Trace($"PriorCertSN={priorCertSn}");
            var priorCert = certificateDataReader.GetCertificateRecord(DataConversion.HexToBytes(priorCertSn));
            if (priorCert == null)
            {
                Logger.Error($"Prior certificate not found for PriorCertSN={priorCertSn}");
                throw new InvalidOperationException($"Prior certificate record not found for serial number '{priorCertSn}'.");
            }

            Logger.Trace($"Prior Cert CARequestID={priorCert.CARequestID ?? "(null)"}");
            if (string.IsNullOrEmpty(priorCert.CARequestID))
            {
                Logger.Error("Prior certificate has null/empty CARequestID.");
                throw new InvalidOperationException("Prior certificate has no CA Request ID.");
            }

            //CARequestID is {SSL Store order id}-{serial number}, several certificates can share an order
            var orderId = priorCert.CARequestID.Split('-')[0];
            flow.Step("ResolvePriorCertificate", $"order={orderId}");

            var orderStatusRequest = _requestManager.GetOrderStatusRequest(orderId);
            Logger.Trace($"orderStatusRequest JSON {LogRedactor.Json(orderStatusRequest)}");

            var orderStatusResponse = flow.Step("FetchPriorOrderStatus",
                () => RunSync(() => SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)), $"order={orderId}");
            if (orderStatusResponse == null)
            {
                Logger.Error("Order status response is null for the prior order.");
                throw new InvalidOperationException($"Failed to retrieve the status of prior order {orderId}.");
            }

            Logger.Trace($"orderStatusResponse JSON {LogRedactor.Json(orderStatusResponse)}");
            return orderStatusResponse;
        }

        /// <summary>
        ///     Resolves New / Renew / Reissue from the prior certificate's order expiry rather than the certificate's own
        ///     expiration, so shorter-lived certificates under an active order are reissued, not renewed.
        ///       explicit Reissue                          -> Reissue
        ///       order beyond the renewal window           -> Reissue (under the active order)
        ///       order inside the window, not yet expired  -> Renew
        ///       order expired                             -> New
        ///     Falls back to the certificate's expiration when the order expiry is unavailable. If nothing can be
        ///     evaluated the requested type is kept.
        /// </summary>
        private RequestUtilities.EnrollmentType ResolveEnrollmentType(FlowLogger flow, INewOrderResponse priorOrder,
            RequestUtilities.EnrollmentType requested)
        {
            if (requested == RequestUtilities.EnrollmentType.Reissue)
            {
                flow.Skip("EvaluateOrderValidity", "explicit Reissue request");
                return requested;
            }

            try
            {
                if (TryGetOrderExpiry(priorOrder, out var orderExpiry, out var source))
                {
                    var daysLeft = (orderExpiry - DateTime.UtcNow).TotalDays;
                    var resolved = daysLeft > RenewalWindowDays ? RequestUtilities.EnrollmentType.Reissue
                        : daysLeft >= 0 ? RequestUtilities.EnrollmentType.Renew
                        : RequestUtilities.EnrollmentType.New;
                    flow.Step("EvaluateOrderValidity",
                        $"orderExpiry={orderExpiry:dd-MMM-yyyy} ({source}), daysLeft={(int) daysLeft}, window={RenewalWindowDays}, result={resolved}");
                    return resolved;
                }

                if (TryParseSslStoreDate(priorOrder.CertificateEndDateInUtc, priorOrder.CertificateEndDate,
                        out var certExpiry))
                {
                    var resolved = certExpiry >= DateTime.UtcNow ? RequestUtilities.EnrollmentType.Reissue : requested;
                    flow.Step("EvaluateOrderValidity",
                        $"order expiry unavailable, using certificate expiration {certExpiry:dd-MMM-yyyy}, result={resolved}");
                    return resolved;
                }

                flow.Skip("EvaluateOrderValidity", "neither order expiry nor certificate expiration available");
                return requested;
            }
            catch (Exception e)
            {
                flow.Fail("EvaluateOrderValidity", e.Message);
                Logger.Warn($"Unable to evaluate the prior order, keeping requested type {requested}: {e.Message}");
                return requested;
            }
        }

        private static bool TryGetOrderExpiry(INewOrderResponse order, out DateTime expiryUtc, out string source)
        {
            if (TryParseSslStoreDate(order.OrderExpiryDateInUtc, order.OrderExpiryDate, out expiryUtc))
            {
                source = "OrderExpiryDate";
                return true;
            }

            //No order expiry returned, the order runs for its validity (in months) from the purchase date
            if (order.Validity > 0 && TryParseSslStoreDate(order.PurchaseDateInUtc, order.PurchaseDate, out var purchased))
            {
                expiryUtc = purchased.AddMonths(order.Validity);
                source = $"PurchaseDate+{order.Validity}mo";
                return true;
            }

            source = null;
            return false;
        }

        /// <summary>
        ///     Parses a date as UTC, preferring the explicit UTC value. Culture-safe: tries invariant first.
        /// </summary>
        private static bool TryParseSslStoreDate(string utcValue, string localValue, out DateTime utc)
        {
            foreach (var value in new[] {utcValue, localValue})
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, styles, out utc) ||
                    DateTime.TryParse(value, CultureInfo.CurrentCulture, styles, out utc))
                    return true;
            }

            utc = default(DateTime);
            return false;
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
            //Most orders wait on domain validation (approver email, CNAME), so no certificate comes back yet. Return the
            //order id so the gateway keeps the request and polls GetSingleRecord until it is issued.
            return new EnrollmentResult
            {
                Status = Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.EXTERNAL_VALIDATION),
                CARequestID = newOrderResponse.TheSslStoreOrderId,
                StatusMessage = $"Order Successfully Created With Order Number {newOrderResponse.TheSslStoreOrderId}"
            };
        }


        public override CAConnectorCertificate GetSingleRecord(string caRequestId)
        {
            using (var flow = NewFlow("GetSingleRecord"))
            {
                return GetSingleRecordInternal(flow, caRequestId);
            }
        }

        private CAConnectorCertificate GetSingleRecordInternal(FlowLogger flow, string caRequestId)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            Logger.Trace($"GetSingleRecord called: caRequestId={caRequestId ?? "(null)"}");

            try
            {
                if (string.IsNullOrEmpty(caRequestId))
                {
                    Logger.Error("GetSingleRecord called with null/empty caRequestId.");
                    flow.Fail("ValidateInput", "caRequestId is null or empty");
                    Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                    return new CAConnectorCertificate
                    {
                        CARequestID = caRequestId,
                        Certificate = string.Empty,
                        CSR = string.Empty,
                        Status = Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED)
                    };
                }

                //CARequestID is {order id} for a new order that is still pending, {order id}-{serial} once synced
                var orderId = caRequestId.Split('-')[0];
                flow.Step("ValidateInput", $"caRequestId={caRequestId}, order={orderId}");
                var orderStatusRequest = _requestManager.GetOrderStatusRequest(orderId);

                Logger.Trace($"orderStatusRequest JSON {LogRedactor.Json(orderStatusRequest)}");

                var certResponse = flow.Step("FetchOrderStatus",
                    () => RunSync(() => SslStoreClient.SubmitOrderStatusRequestAsync(orderStatusRequest)));

                Logger.Trace($"certResponse JSON {LogRedactor.Json(certResponse)}");

                var majorStatus = certResponse?.OrderStatus?.MajorStatus;
                Logger.Trace($"GetSingleRecord: MajorStatus={majorStatus ?? "(null)"}");
                var status = _requestManager.MapReturnStatus(majorStatus);
                flow.Step("MapStatus", $"majorStatus={majorStatus ?? "(null)"}");

                var certificate = string.Empty;
                if (status == Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.ISSUED))
                    certificate = flow.Step("DownloadCertificate",
                        () => DownloadCertificateContent(orderId, certResponse.CommonName));
                else
                    flow.Skip("DownloadCertificate", "order is not issued yet (pending validation)");

                var purchaseDate = DateTime.UtcNow;
                if (TryParseSslStoreDate(certResponse?.PurchaseDateInUtc, certResponse?.PurchaseDate, out var purchased))
                    purchaseDate = purchased;

                Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
                return new CAConnectorCertificate
                {
                    CARequestID = caRequestId,
                    Certificate = certificate,
                    CSR = string.Empty,
                    Status = status,
                    SubmissionDate = purchaseDate,
                    ProductID = certResponse?.ProductCode
                };
            }
            catch (Exception ex)
            {
                flow.Fail("GetSingleRecord", ex.Message);
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

        /// <summary>
        ///     Downloads the issued certificate for an order. Returns an empty string when SSL Store has none to give.
        /// </summary>
        private string DownloadCertificateContent(string orderId, string commonName)
        {
            var downloadRequest = _requestManager.GetCertificateRequest(orderId);
            var response = RunSync(() => SslStoreClient.SubmitDownloadCertificateAsync(downloadRequest));
            if (response?.AuthResponse == null || response.AuthResponse.IsError)
            {
                Logger.Warn($"Certificate download for order {orderId} returned no certificate: {LogRedactor.Json(response?.AuthResponse)}");
                return string.Empty;
            }

            return _requestManager.GetCertificateContent(response.Certificates, commonName) ?? string.Empty;
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
            using (var flow = NewFlow("Synchronize"))
            {
                SynchronizeInternal(flow, certificateDataReader, blockingBuffer, certificateAuthoritySyncInfo, cancelToken);
            }
        }

        private void SynchronizeInternal(FlowLogger flow, ICertificateDataReader certificateDataReader,
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
                flow.Step("QueryTemplates", "template query submitted");

                //Get a list of orgs from digicert via SslStore API
                Logger.Trace("Getting organization list request...");
                var organizationListRequest = _requestManager.GetOrganizationListRequest();

                Logger.Trace($"organizationListRequest JSON {LogRedactor.Json(organizationListRequest)}");

                Logger.Trace("Submitting organization list request to SslStore...");
                var orgListResponse = Task
                    .Run(async () => await SslStoreClient.SubmitOrganizationListAsync(organizationListRequest)).Result;

                Logger.Trace($"orgListResponse JSON {LogRedactor.Json(orgListResponse)}");

                if (orgListResponse == null)
                {
                    Logger.Warn("Organization list response is null. Skipping template organization updates.");
                    flow.Fail("FetchOrganizations", "response is null, template organization updates skipped");
                }
                else
                {
                    flow.Step("FetchOrganizations", $"count={orgListResponse.OrganizationList?.Count ?? 0}");
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
                            Logger.Trace($"Template update response for {currentTemplate.CommonName}: {LogRedactor.Json(updateOrgResponse)}");
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
                flow.Step("ProcessTemplates");
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

                        Logger.Trace($"Order status response for {orderId}: OrderStatus={LogRedactor.Json(orderStatusResponse.OrderStatus)}, CommonName={orderStatusResponse.CommonName ?? "(null)"}, ProductCode={orderStatusResponse.ProductCode ?? "(null)"}");

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
                                    Logger.Warn($"Certificate download returned error for order {orderId}: {LogRedactor.Json(certResponse.AuthResponse)}");
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
                var syncSummary = $"processed={certCount}, added={addedCount}, errors={errorCount}";
                if (errorCount > 0) flow.Fail("SyncCertificates", syncSummary);
                else flow.Step("SyncCertificates", syncSummary);
            }
            catch (AggregateException aggEx)
            {
                var flatEx = aggEx.Flatten();
                flow.Fail("Synchronize", flatEx.Message);
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
                flow.Fail("Synchronize", ex.Message);
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
            using (var flow = NewFlow("Initialize"))
            {
                InitializeInternal(flow, configProvider);
            }
        }

        private void InitializeInternal(FlowLogger flow, ICAConnectorConfigProvider configProvider)
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

                flow.Step("ValidateConfig", "required keys present");
                PartnerCode = configProvider.CAConnectionData[Constants.PartnerCode]?.ToString();
                Logger.Trace($"Initialize: PartnerCode {(string.IsNullOrEmpty(PartnerCode) ? "is empty" : "set (value masked)")}.");

                AuthenticationToken = configProvider.CAConnectionData[Constants.AuthToken]?.ToString();
                Logger.Trace("Initialize: AuthenticationToken set (value masked).");

                Logger.Trace("Initialize: Creating SslStoreClient...");
                SslStoreClient = new SslStoreClient(configProvider);

                Logger.Trace("Initialize: Creating KeyfactorClient...");
                KeyfactorClient = new KeyfactorClient(configProvider);

                Logger.Trace("Initialize: Getting page size...");
                PageSize = _requestManager.GetClientPageSize(configProvider);
                Logger.Trace($"Initialize: PageSize={PageSize}");

                flow.Step("CreateClients", $"pageSize={PageSize}");

                RenewalWindowDays = Constants.DefaultRenewalWindowDays;
                if (configProvider.CAConnectionData.TryGetValue(Constants.RenewalWindowDays, out var renewalWindow))
                {
                    if (int.TryParse(renewalWindow?.ToString(), out var renewalWindowDays) && renewalWindowDays > 0)
                    {
                        RenewalWindowDays = renewalWindowDays;
                    }
                    else
                    {
                        Logger.Warn($"{Constants.RenewalWindowDays} value '{renewalWindow}' could not be parsed or was <= 0, using default {Constants.DefaultRenewalWindowDays}.");
                        flow.Fail("ReadRenewalWindowDays", $"invalid value, using default {Constants.DefaultRenewalWindowDays}");
                    }
                }

                flow.Step("ReadRenewalWindowDays", $"days={RenewalWindowDays}");

                ConfigManager = configProvider;

                Logger.Trace("Initialize complete.");
            }
            catch (Exception ex)
            {
                flow.Fail("Initialize", ex.Message);
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