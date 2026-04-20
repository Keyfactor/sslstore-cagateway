using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using CAProxy.AnyGateway.Interfaces;
using CAProxy.AnyGateway.Models;
using CSS.Common.Logging;
using CSS.PKI;
using CSS.PKI.PEM;
using Keyfactor.AnyGateway.SslStore.Client.Models;
using Keyfactor.AnyGateway.SslStore.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Keyfactor.AnyGateway.SslStore
{
    public class RequestManager : LoggingClientBase, IRequestManager
    {
        private readonly SslStoreCaProxy _sslStoreCaProxy;

        public RequestManager(SslStoreCaProxy sslStoreCaProxy)
        {
            _sslStoreCaProxy = sslStoreCaProxy;
        }


        public NewOrderRequest GetEnrollmentRequest(string csr, EnrollmentProductInfo productInfo,
            ICAConnectorConfigProvider configProvider, bool isRenewalOrder)
        {
            Logger.Trace($"GetEnrollmentRequest called: isRenewalOrder={isRenewalOrder}, ProductID={productInfo?.ProductID ?? "(null)"}");
            try
            {
                if (string.IsNullOrEmpty(csr))
                {
                    Logger.Error("GetEnrollmentRequest: CSR is null or empty.");
                    throw new ArgumentNullException(nameof(csr), "CSR is required.");
                }

                csr = PemUtilities.DERToPEM(Convert.FromBase64String(csr), PemUtilities.PemObjectType.CertRequest);
                Logger.Trace("GetEnrollmentRequest: CSR converted to PEM.");

                if (configProvider?.CAConnectionData == null || !configProvider.CAConnectionData.ContainsKey("SampleRequest"))
                {
                    Logger.Error("GetEnrollmentRequest: configProvider CAConnectionData missing or 'SampleRequest' key not found.");
                    throw new InvalidOperationException("SampleRequest configuration is missing.");
                }

                var sampleRequestObj = configProvider.CAConnectionData["SampleRequest"];
                Logger.Trace($"GetEnrollmentRequest: SampleRequest type={sampleRequestObj?.GetType().Name ?? "(null)"}");

                var sampleRequest = JsonConvert.SerializeObject(sampleRequestObj);
                Logger.Trace($"GetEnrollmentRequest: SampleRequest JSON length={sampleRequest?.Length ?? 0}");

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                var templateRequest = JsonConvert.DeserializeObject<TemplateNewOrderRequest>(sampleRequest, settings);
                if (templateRequest == null)
                {
                    Logger.Error("GetEnrollmentRequest: Failed to deserialize SampleRequest into TemplateNewOrderRequest.");
                    throw new InvalidOperationException("Failed to deserialize SampleRequest.");
                }

                var request = BuildNewOrderRequest(productInfo, templateRequest, csr, isRenewalOrder);
                Logger.Trace($"GetEnrollmentRequest: Request built successfully.");
                return request;
            }
            catch (Exception ex)
            {
                Logger.Error($"GetEnrollmentRequest failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public EmailApproverRequest GetEmailApproverListRequest(string productId, string productName)
        {
            return new EmailApproverRequest()
            {
                AuthRequest = GetAuthRequest(),
                ProductCode = productId,
                DomainName = productName
            };
        }

        public OrganizationListRequest GetOrganizationListRequest()
        {
            return new OrganizationListRequest()
            {
                PartnerCode = _sslStoreCaProxy.PartnerCode,
                AuthToken = _sslStoreCaProxy.AuthenticationToken
            };
        }

        public AuthRequest GetAuthRequest()
        {
            return new AuthRequest
            {
                PartnerCode = _sslStoreCaProxy.PartnerCode,
                AuthToken = _sslStoreCaProxy.AuthenticationToken
            };
        }

        public ReIssueRequest GetReIssueRequest(INewOrderResponse orderData, string csr, bool isRenewal)
        {
            Logger.Trace($"GetReIssueRequest called: OrderId={orderData?.TheSslStoreOrderId ?? "(null)"}, isRenewal={isRenewal}");

            if (orderData == null)
            {
                Logger.Error("GetReIssueRequest: orderData is null.");
                throw new ArgumentNullException(nameof(orderData));
            }

            var productCode = orderData.ProductCode ?? "";
            Logger.Trace($"GetReIssueRequest: ProductCode={productCode}, AdminContact is null={orderData.AdminContact == null}, OrderStatus is null={orderData.OrderStatus == null}");

            return new ReIssueRequest
            {
                AuthRequest = GetAuthRequest(),
                TheSslStoreOrderId = orderData.TheSslStoreOrderId,
                Csr = csr,
                IsRenewalOrder = isRenewal,
                IsWildCard = productCode.Contains("wc") || productCode.Contains("wildcard"),
                ReissueEmail = orderData.AdminContact?.Email,
                ApproverEmails = orderData.ApproverEmail,
                PreferEnrollmentLink = false,
                FileAuthDvIndicator = orderData.OrderStatus?.DomainAuthVettingStatus == null ? false : orderData.OrderStatus.DomainAuthVettingStatus.Exists(x => x.FileName != null),
                CNameAuthDvIndicator = orderData.OrderStatus?.DomainAuthVettingStatus == null ? false : orderData.OrderStatus.DomainAuthVettingStatus.Exists(x => x.DnsName != null),
                WebServerType = orderData.WebServerType
            };
        }

        public AdminContact GetAdminContact(EnrollmentProductInfo productInfo)
        {
            Logger.Trace("GetAdminContact(EnrollmentProductInfo) called.");

            if (productInfo?.ProductParameters == null)
            {
                Logger.Error("GetAdminContact: productInfo or ProductParameters is null.");
                throw new ArgumentNullException(nameof(productInfo));
            }

            string GetParam(string key)
            {
                if (productInfo.ProductParameters.ContainsKey(key))
                    return productInfo.ProductParameters[key];
                Logger.Warn($"GetAdminContact: Missing parameter '{key}'.");
                return null;
            }

            return new AdminContact
            {
                FirstName = GetParam("Admin Contact - First Name"),
                LastName = GetParam("Admin Contact - Last Name"),
                Phone = GetParam("Admin Contact - Phone"),
                Email = GetParam("Admin Contact - Email"),
                OrganizationName = GetParam("Admin Contact - Organization Name"),
                AddressLine1 = GetParam("Admin Contact - Address"),
                City = GetParam("Admin Contact - City"),
                Region = GetParam("Admin Contact - Region"),
                PostalCode = GetParam("Admin Contact - Postal Code"),
                Country = GetParam("Admin Contact - Country")
            };
        }


        public TechnicalContact GetTechnicalContact(EnrollmentProductInfo productInfo)
        {
            Logger.Trace("GetTechnicalContact(EnrollmentProductInfo) called.");

            if (productInfo?.ProductParameters == null)
            {
                Logger.Error("GetTechnicalContact: productInfo or ProductParameters is null.");
                throw new ArgumentNullException(nameof(productInfo));
            }

            string GetParam(string key)
            {
                if (productInfo.ProductParameters.ContainsKey(key))
                    return productInfo.ProductParameters[key];
                Logger.Warn($"GetTechnicalContact: Missing parameter '{key}'.");
                return null;
            }

            return new TechnicalContact
            {
                FirstName = GetParam("Technical Contact - First Name"),
                LastName = GetParam("Technical Contact - Last Name"),
                Phone = GetParam("Technical Contact - Phone"),
                Email = GetParam("Technical Contact - Email"),
                OrganizationName = GetParam("Technical Contact - Organization Name"),
                AddressLine1 = GetParam("Technical Contact - Address"),
                City = GetParam("Technical Contact - City"),
                Region = GetParam("Technical Contact - Region"),
                PostalCode = GetParam("Technical Contact - Postal Code"),
                Country = GetParam("Technical Contact - Country")
            };
        }

        public DownloadCertificateRequest GetCertificateRequest(string theSslStoreOrderId)
        {
            return new DownloadCertificateRequest
            {
                AuthRequest = GetAuthRequest(),
                TheSslStoreOrderId = theSslStoreOrderId
            };
        }

        public RevokeOrderRequest GetRevokeOrderRequest(string theSslStoreOrderId)
        {
            return new RevokeOrderRequest
            {
                AuthRequest = GetAuthRequest(),
                TheSslStoreOrderId = theSslStoreOrderId
            };
        }

        public int GetClientPageSize(ICAConnectorConfigProvider config)
        {
            Logger.Trace("GetClientPageSize called.");
            try
            {
                if (config?.CAConnectionData == null)
                {
                    Logger.Warn("GetClientPageSize: config or CAConnectionData is null. Using default page size.");
                    return Constants.DefaultPageSize;
                }

                if (config.CAConnectionData.ContainsKey(Constants.PageSize))
                {
                    var pageSizeValue = config.CAConnectionData[Constants.PageSize]?.ToString();
                    Logger.Trace($"GetClientPageSize: Raw value='{pageSizeValue ?? "(null)"}'");

                    if (int.TryParse(pageSizeValue, out var pageSize))
                    {
                        Logger.Trace($"GetClientPageSize: Parsed PageSize={pageSize}");
                        return pageSize;
                    }

                    Logger.Warn($"GetClientPageSize: Failed to parse '{pageSizeValue}' as int. Using default.");
                    return Constants.DefaultPageSize;
                }

                Logger.Trace($"GetClientPageSize: '{Constants.PageSize}' key not found. Using default={Constants.DefaultPageSize}");
                return Constants.DefaultPageSize;
            }
            catch (Exception ex)
            {
                Logger.Error($"GetClientPageSize failed: {ex.Message}\n{ex.StackTrace}");
                return Constants.DefaultPageSize;
            }
        }

        public QueryOrderRequest GetQueryOrderRequest(int pageSize, int pageNumber)
        {
            return new QueryOrderRequest
            {
                AuthRequest = GetAuthRequest(),
                PageSize = pageSize,
                PageNumber = pageNumber
            };
        }

        public OrderStatusRequest GetOrderStatusRequest(string theSslStoreId)
        {
            return new OrderStatusRequest
            {
                AuthRequest = GetAuthRequest(),
                TheSslStoreOrderId = theSslStoreId
            };
        }

        public int MapReturnStatus(string sslStoreStatus)
        {
            Logger.Trace($"MapReturnStatus called: sslStoreStatus={sslStoreStatus ?? "(null)"}");

            if (sslStoreStatus == null)
            {
                Logger.Warn("MapReturnStatus: sslStoreStatus is null, returning UNKNOWN.");
                return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.UNKNOWN);
            }

            PKIConstants.Microsoft.RequestDisposition returnStatus;

            switch (sslStoreStatus)
            {
                case "Active":
                    returnStatus = PKIConstants.Microsoft.RequestDisposition.ISSUED;
                    break;
                case "Initial":
                case "Pending":
                    returnStatus = PKIConstants.Microsoft.RequestDisposition.EXTERNAL_VALIDATION;
                    break;
                case "Cancelled":
                    returnStatus = PKIConstants.Microsoft.RequestDisposition.REVOKED;
                    break;
                default:
                    Logger.Warn($"MapReturnStatus: Unrecognized status '{sslStoreStatus}', returning UNKNOWN.");
                    returnStatus = PKIConstants.Microsoft.RequestDisposition.UNKNOWN;
                    break;
            }

            Logger.Trace($"MapReturnStatus: Mapped '{sslStoreStatus}' to {returnStatus} ({Convert.ToInt32(returnStatus)})");
            return Convert.ToInt32(returnStatus);
        }

        public NewOrderRequest GetRenewalRequest(INewOrderResponse orderData, string csr)
        {
            Logger.Trace($"GetRenewalRequest called: OrderId={orderData?.TheSslStoreOrderId ?? "(null)"}");

            if (orderData == null)
            {
                Logger.Error("GetRenewalRequest: orderData is null.");
                throw new ArgumentNullException(nameof(orderData));
            }

            Logger.Trace($"GetRenewalRequest: ProductCode={orderData.ProductCode ?? "(null)"}, AdminContact is null={orderData.AdminContact == null}, OrderStatus is null={orderData.OrderStatus == null}");

            return new NewOrderRequest
            {
                AuthRequest = GetAuthRequest(),
                RelatedTheSslStoreOrderId = orderData.TheSslStoreOrderId,
                ProductCode = orderData.ProductCode,
                AdminContact = GetAdminContact(orderData),
                TechnicalContact = GetTechnicalContact(orderData),
                ApproverEmail = orderData.ApproverEmail,
                SignatureHashAlgorithm = orderData.SignatureHashAlgorithm,
                WebServerType = orderData.WebServerType,
                ValidityPeriod = orderData.Validity,
                ServerCount = orderData.ServerCount,
                IsRenewalOrder = true,
                FileAuthDvIndicator = orderData.OrderStatus?.DomainAuthVettingStatus?.Exists(x => x.FileName != null),
                CnameAuthDvIndicator = orderData.OrderStatus?.DomainAuthVettingStatus?.Exists(x => x.DnsName != null),
                Csr = csr
            };
        }

        public AdminContact GetAdminContact(INewOrderResponse productInfo)
        {
            Logger.Trace("GetAdminContact(INewOrderResponse) called.");

            if (productInfo?.AdminContact == null)
            {
                Logger.Warn("GetAdminContact: productInfo or AdminContact is null, returning empty AdminContact.");
                return new AdminContact();
            }

            return new AdminContact
            {
                FirstName = productInfo.AdminContact.FirstName,
                LastName = productInfo.AdminContact.LastName,
                Phone = productInfo.AdminContact.Phone,
                Email = productInfo.AdminContact.Email
            };
        }

        public TechnicalContact GetTechnicalContact(INewOrderResponse productInfo)
        {
            Logger.Trace("GetTechnicalContact(INewOrderResponse) called.");

            if (productInfo?.AdminContact == null)
            {
                Logger.Warn("GetTechnicalContact: productInfo or AdminContact is null, returning empty TechnicalContact.");
                return new TechnicalContact();
            }

            return new TechnicalContact
            {
                FirstName = productInfo.AdminContact.FirstName,
                LastName = productInfo.AdminContact.LastName,
                Phone = productInfo.AdminContact.Phone,
                Email = productInfo.AdminContact.Email
            };
        }

        private NewOrderRequest BuildNewOrderRequest(EnrollmentProductInfo productInfo,
            TemplateNewOrderRequest newOrderRequest, string csr, bool isRenewal)
        {
            Logger.Trace($"BuildNewOrderRequest called: ProductID={productInfo?.ProductID ?? "(null)"}, isRenewal={isRenewal}");
            var customOrderId = Guid.NewGuid().ToString();
            Logger.Trace($"BuildNewOrderRequest: Generated CustomOrderId={customOrderId}");
            productInfo.ProductParameters.Add("CustomOrderId", customOrderId);

            var request =
                new JObject(
                    new JObject(
                        new JProperty("AuthRequest",
                            new JObject(new JProperty("PartnerCode", _sslStoreCaProxy.PartnerCode),
                                new JProperty("AuthToken", _sslStoreCaProxy.AuthenticationToken))),
                        new JProperty("ProductCode", productInfo.ProductID.Replace("-EO", "")),
                        new JProperty("CustomOrderId", customOrderId),
                        new JProperty("TSSOrganizationId", productInfo.ProductParameters.ContainsKey("Organization ID") ? ExtractOrgId(productInfo.ProductParameters["Organization ID"]) : null),
                        new JProperty("OrganizationInfo",
                            new JObject(
                                CreatePropertyFromTemplate("$.OrganizationInfo.OrganizationName", productInfo,
                                    newOrderRequest),
                                CreatePropertyFromTemplate("$.OrganizationInfo.RegistrationNumber", productInfo,
                                    newOrderRequest),
                                CreatePropertyFromTemplate("$.OrganizationInfo.JurisdictionCountry", productInfo,
                                    newOrderRequest),
                                new JProperty("OrganizationAddress",
                                    new JObject(
                                        CreatePropertyFromTemplate(
                                            "$.OrganizationInfo.OrganizationAddress.AddressLine1", productInfo,
                                            newOrderRequest),
                                        CreatePropertyFromTemplate("$.OrganizationInfo.OrganizationAddress.Region",
                                            productInfo, newOrderRequest),
                                        CreatePropertyFromTemplate("$.OrganizationInfo.OrganizationAddress.PostalCode",
                                            productInfo, newOrderRequest),
                                        CreatePropertyFromTemplate("$.OrganizationInfo.OrganizationAddress.Country",
                                            productInfo, newOrderRequest),
                                        CreatePropertyFromTemplate("$.OrganizationInfo.OrganizationAddress.Phone",
                                            productInfo, newOrderRequest),
                                        CreatePropertyFromTemplate(
                                            "$.OrganizationInfo.OrganizationAddress.LocalityName", productInfo,
                                            newOrderRequest))))),
                        CreatePropertyFromTemplate("$.ValidityPeriod", productInfo, newOrderRequest),
                        new JProperty("ServerCount", 1),
                        new JProperty("CSR", csr),
                        CreatePropertyFromTemplate("$.DomainName", productInfo, newOrderRequest),
                        new JProperty("WebServerType", "Other"),
                        CreatePropertyFromTemplate("$.DNSNames", productInfo, newOrderRequest, true),
                        new JProperty("isCUOrder", false),
                        CreatePropertyFromTemplate("$.AutoWWW", productInfo, newOrderRequest),
                        new JProperty("IsRenewalOrder", isRenewal),
                        new JProperty("isTrialOrder", false),
                        new JProperty("AdminContact",
                            new JObject(
                                CreatePropertyFromTemplate("$.AdminContact.FirstName", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.LastName", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.Phone", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.Email", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.Title", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.OrganizationName", productInfo,
                                    newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.AddressLine1", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.City", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.Region", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.PostalCode", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.AdminContact.Country", productInfo, newOrderRequest)
                            )),
                        new JProperty("TechnicalContact",
                            new JObject(
                                CreatePropertyFromTemplate("$.TechnicalContact.FirstName", productInfo,
                                    newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.LastName", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.Phone", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.Email", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.Title", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.OrganizationName", productInfo,
                                    newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.AddressLine1", productInfo,
                                    newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.City", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.Region", productInfo, newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.PostalCode", productInfo,
                                    newOrderRequest),
                                CreatePropertyFromTemplate("$.TechnicalContact.Country", productInfo, newOrderRequest)
                            )),
                        CreatePropertyFromTemplate("$.ApproverEmail", productInfo, newOrderRequest),
                        new JProperty("FileAuthDVIndicator", false),
                        new JProperty("CNAMEAuthDVIndicator", false),
                        new JProperty("SignatureHashAlgorithm", "PREFER_SHA2")));

            return request.ToObject<NewOrderRequest>();
        }

        public string GetCertificateContent(List<Certificate> certificates, string commonName)
        {
            Logger.Trace($"GetCertificateContent called: commonName={commonName ?? "(null)"}, certificates count={certificates?.Count ?? 0}");

            if (certificates == null || certificates.Count == 0)
            {
                Logger.Warn("GetCertificateContent: certificates list is null or empty.");
                return "";
            }

            if (string.IsNullOrEmpty(commonName))
            {
                Logger.Warn("GetCertificateContent: commonName is null or empty.");
                return "";
            }

            foreach (var c in certificates)
            {
                if (c == null)
                {
                    Logger.Warn("GetCertificateContent: Encountered null certificate in list, skipping.");
                    continue;
                }

                if (string.IsNullOrEmpty(c.FileContent))
                {
                    Logger.Warn("GetCertificateContent: Certificate has null/empty FileContent, skipping.");
                    continue;
                }

                try
                {
                    var cert = new X509Certificate2(Encoding.UTF8.GetBytes(c.FileContent));
                    Logger.Trace($"GetCertificateContent: Checking cert SubjectName={cert.SubjectName?.Name ?? "(null)"} against commonName={commonName}");
                    if (cert.SubjectName?.Name != null && cert.SubjectName.Name.Contains(commonName))
                    {
                        Logger.Trace($"GetCertificateContent: Match found for commonName={commonName}");
                        return c.FileContent;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"GetCertificateContent: Error parsing certificate: {ex.Message}\n{ex.StackTrace}");
                }
            }

            Logger.Warn($"GetCertificateContent: No matching certificate found for commonName={commonName}");
            return "";
        }

        private JArray CreateJArrayFromCommaSeparatedList(string csList)
        {
            var ja = new JArray();
            foreach (var i in csList.Split(',')) ja.Add(i);

            return ja;
        }


        private JProperty CreatePropertyFromTemplate(string propertyPath, EnrollmentProductInfo productInfo,
            TemplateNewOrderRequest newOrderRequest, bool isArray = false)
        {
            var template = (JObject)JToken.FromObject(newOrderRequest);
            var requiredForProducts =
                new JArray(template.SelectTokens(propertyPath + ".FieldData.RequiredForProducts"));
            var enrollmentFieldName = template.SelectToken(propertyPath + ".FieldData.EnrollmentFieldMapping");

            if (requiredForProducts.Count > 0)
            {
                //see if this is required for all products or the productId from the template you are using
                if (requiredForProducts[0].Any(i => i.Value<string>() == "All") ||
                    requiredForProducts[0].Any(i => i.Value<string>() == productInfo.ProductID))
                    if (enrollmentFieldName != null && enrollmentFieldName.Value<string>() != "None")
                    {
                        if (productInfo.ProductParameters.ContainsKey(enrollmentFieldName.Value<string>()))
                        {
                            var enrollmentFieldValue =
                                productInfo.ProductParameters[enrollmentFieldName.Value<string>()];
                            if (isArray == false)
                                return new JProperty(propertyPath.Substring(propertyPath.LastIndexOf('.') + 1),
                                    enrollmentFieldValue);
                            return new JProperty(propertyPath.Substring(propertyPath.LastIndexOf('.') + 1),
                                CreateJArrayFromCommaSeparatedList(enrollmentFieldValue));
                        }

                        Logger.Error(
                            $"Enrollment Field is required in the config settings but missing from the request or names do not match.: {enrollmentFieldName.Value<string>()}");
                    }
            }
            else
            {
                Logger.Error($"Enrollment Field is in the request but missing from config settings: {propertyPath}");
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            return new JProperty(propertyPath.Substring(propertyPath.LastIndexOf('.') + 1), null);
        }

        private string ExtractOrgId(string organization)
        {
            Logger.Trace($"ExtractOrgId called: organization={organization ?? "(null)"}");
            if (organization != null)
            {
                Regex pattern = new Regex(@"(\([^0-9]*\d+[^0-9]*\))");
                Match match = pattern.Match(organization);
                var result = match.Value.Replace("(", "").Replace(")", "");
                Logger.Trace($"ExtractOrgId: Extracted orgId='{result}'");
                return result;
            }
            else
            {
                Logger.Warn("ExtractOrgId: organization is null, returning null.");
                return null;
            }
        }
    }
}