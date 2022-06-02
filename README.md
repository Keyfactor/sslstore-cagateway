# SSLStore

SSLStore is a certificate reseller with access to over 80 certificate products. Vendors include Digicert and Sectigo and all their acquired companies such as RapidSSL, Geotrust and Comodo. There is one API for all these products so a single integration to the SSLStore can get you instant access to over 80 Certificate products.

#### Integration status: Production - Ready for use in production environments.

## About the Keyfactor AnyGateway CA Connector

This repository contains an AnyGateway CA Connector, which is a plugin to the Keyfactor AnyGateway. AnyGateway CA Connectors allow Keyfactor Command to be used for inventory, issuance, and revocation of certificates from a third-party certificate authority.

---

*** 

# Compatibility
This AnyGateway is designed to be used with version 21.3.2 of the Keyfactor AnyGateway Framework.

# Getting Started

## Integration Overview

### Supported Functionality
- Certificate Sync - Full
- Certificate Enrollment for Domain Validated product suite (Regular, with SANs, and Wildcard)
- Certificate Enrollment for Organization Validated product suite (Regular, with SANs, and Wildcard)
- Certificate Enrollment for Extended Validation product suite (Regular and with SANs)
- Certificate Renewal/Reissue
- Certificate Revocation


### Unsupported Functionality
- Certificate Sync - Partial (not possible through the SSL Store API library)
- Approval/Denial of Enrollment Requests (not possible through the SSL Store API library)

### Documentation
**General Documentation**
[SSLStore API Documentation](https://www.thesslstore.com/api/)


## Standard Gateway Installation
To begin, you must have the CA Gateway Service 21.3.2 installed and operational before attempting to configure the SSLStore Any Gateway plugin. This integration was tested with Keyfactor 8.7.0.0.
To install the gateway follow these instructions.

1) Gateway Server - run the installation .msi obtained from Keyfactor

2) Gateway Server - If you have the rights to install the database (usually in a Non SQL PAAS Environment) Using Powershell, run the following command to create the gateway database.

   **SQL Server Windows Auth**
    ```
    %InstallLocation%\DatabaseManagementConsole.exe create -s [database server name] -d [database name]
    ```
   Note if you are using SQL Authentication, then you need to run
   
   **SQL Server SQL Authentication**

   ```
   %InstallLocation%\DatabaseManagementConsole.exe create -s [database server name] -d [database name] -u [sql user] -p [sql password]
   ```

   If you do **not** have rights to created the database then have the database created ahead of time by the support team and just populate the database

   ## Populate commands below

   **Windows Authentication**

   ```
   %InstallLocation%\DatabaseManagementConsole.exe populate -s [database server name] -d [database name]
   ```

   **SQL Server SQL Authentication** 

   ```
   %InstallLocation%\DatabaseManagementConsole.exe populate -s [database server name] -d [database name] -u [sql user] -p [sql password]
   ```

3) Gateway Server - run the following Powershell to import the Cmdlets

   C:\Program Files\Keyfactor\Keyfactor AnyGateway\ConfigurationCmdlets.dll (must be imported into Powershell)
   ```ps
   Import-Module C:\Program Files\Keyfactor\Keyfactor AnyGateway\ConfigurationCmdlets.dll
   ```

4) Gateway Server - Run the Following Powershell script to set the gateway encryption cert

   ### Set-KeyfactorGatewayEncryptionCert
   This cmdlet will generate a self-signed certificate used to encrypt the database connection string. It populates a registry value with the serial number of the certificate to be used. The certificate is stored in the LocalMachine Personal Store and the registry key populated is:

   HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\CertSvcProxy\Parameters\EncryptSerialNumber
   No parameters are required to run this cmdlet.

5) Gateway Server - Run the following Powershell Script to Set the Database Connection

   ### Set-KeyfactorGatewayDatabaseConnection
   This cmdlet will set and encrypt the database connection string used by the AnyGateway service. 

   **Windows Authentication**
   ```ps
   Set-KeyfactorGatewayDatabaseConnection -Server [db server name] -Database [database name]
   ```

   **SQL Authentication**
   ```ps
   $KeyfactorCredentials = Get-Credentials
   Set-KeyfactorGatewayDatabaseConnection -Server [db server name] -Database [database name] -Account [$KeyfactorCredentials]
   ```
## Standard Gateway Configuration Finished
---


## SSLStore AnyGateway Specific Configuration
It is important to note that importing the  SSLStore configuration into the CA Gateway prior to installing the binaries must be completed. Additionally, the CA Gateway service
must be running in order to succesfully import the configuation. When the CA Gateway service starts it will attempt to validate the connection information to 
the CA.  Without the imported configuration, the service will fail to start.

### Binary Installation

1) Get the Latest Zip File from [Here](https://github.com/Keyfactor/quovadis-cagateway/releases)
2) Gateway Server - Copy the SSLStoreCaProxy.dll to the location where the Gateway Framework was installed (usually C:\Program Files\Keyfactor\Keyfactor AnyGateway)

### Configuration Changes
1) Gateway Server - Edit the CAProxyServer.exe.config file and replace the line that says "NoOp" with the line below:
   ```
   <alias alias="CAConnector" type="Keyfactor.AnyGateway.SslStore.SslStoreCaProxy, SslStoreCaProxy"/>
   ```
2) Gateway Server - Install the Intermediate Comodo Certificate that was received from SSLStore

3) Gateway Server - Take the sample Config.json located [Here](https://github.com/Keyfactor/quovadis-cagateway/raw/main/SampleConfig.json) and make the following modifications

- *Security Settings Modifications* (Swap this out for the typical Gateway Security Settings for Test or Prod)

```
  "Security": {
    "KEYFACTOR\\administrator": {
      "READ": "Allow",
      "ENROLL": "Allow",
      "OFFICER": "Allow",
      "ADMINISTRATOR": "Allow"
    },
    "KEYFACTOR\\SVC_AppPool": {
      "READ": "Allow",
      "ENROLL": "Allow",
      "OFFICER": "Allow",
      "ADMINISTRATOR": "Allow"
    },
    "KEYFACTOR\\SVC_TimerService": {
      "READ": "Allow",
      "ENROLL": "Allow",
      "OFFICER": "Allow",
      "ADMINISTRATOR": "Allow"
    }
```
- *SSLStore Environment Settings* (Modify these with the keys and Urls obtained from SSLStore)
```
  "CAConnection": {
    "SSLStoreURL": "https://sandbox-wbapi.thesslstore.com",
    "PartnerCode": "SomePartnerCodeFromSSLStore",
    "AuthToken": "SomeAuthTokenFromSSLStore",
    "KeyfactorApiUrl": "https://kftrain.keyfactor.lab/KeyfactorAPI",
    "KeyfactorApiUserId": "SomeKeyfactorAPIUser",
    "KeyfactorApiPassword": "SomeKeyfactorAPIPassword",
    "PageSize": "25",
    "SampleRequest": {
      "AuthRequest": {
        "PartnerCode": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "None"
          }
        },
        "AuthToken": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "None"
          }
        }
      },
      "ProductCode": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "None"
        }
      },
      "TSSOrganizationId": {
        "FieldData": {
          "RequiredForProducts": [
            "None"
          ],
          "EnrollmentFieldMapping": "Organization ID"
        }
      },
      "OrganizationInfo": {
        "OrganizationName": {
          "FieldData": {
            "RequiredForProducts": [
              "digi_plus_ssl",
              "digi_wc_ssl",
              "digi_md_ssl",
              "digi_securesite_md",
              "digi_csc",
              "digi_securesite",
              "digi_securesite_pro",
              "digi_securesite_wc",
              "digi_securesite_pro_ev",
              "digi_csc_ev",
              "digi_ev_md_ssl",
              "digi_securesite_ev_md",
              "digi_securesite_ev",
              "digi_ssl_ev_basic",
              "digi_securesite_ev_flex",
              "digi_securesite_pro_ev_flex",
              "digi_securesite_pro_flex",
              "digi_securesite_pro_ev_flex",
              "digi_ssl_basic",
              "digi_securesite_flex",
              "digi_plus_ev_ssl",
              "digi_doc_signing_org_2000",
              "digi_doc_signing_org_5000",
              "digi_truebizid_ev",
              "digi_truebizid",
              "digi_truebizid_ev_md",
              "digi_truebizid_wc",
              "digi_truebizid_md",
              "digi_truebizid_md_wc",
              "digi_truebizid_flex",
              "digi_truebizid_ev_flex",
              "digi_sslwebserver_ev",
              "digi_sslwebserver",
              "digi_sslwebserver_wc",
              "digi_sslwebserver_md_wc",
              "digi_sslwebserver_flex",
              "digi_sslwebserver_ev_flex",
              "truebizid",
              "truebusinessidev",
              "truebusinessidevmd",
              "truebusinessidwildcard",
              "truebizidmdwc",
              "truebizidmd",
              "malwarescan",
              "sslwebserverwildcard",
              "thawtecsc",
              "sslwebserver",
              "sslwebserverev",
              "sectigocsc",
              "sectigoevssl",
              "sectigoevcsc",
              "sectigoevmdc",
              "sectigoovssl",
              "sectigomdc",
              "sectigomdcwildcard",
              "sectigoovwildcard",
              "comodocsc",
              "positiveevssl",
              "comodoevssl",
              "comodoevcsc",
              "enterpriseproev",
              "enterpriseproevmdc",
              "positiveevmdc",
              "comodoevmdc",
              "comodomdc",
              "instantssl",
              "instantsslpro",
              "comodopremiumssl",
              "comodomdcwildcard",
              "comodopremiumwildcard",
              "comodouccwildcard",
              "comodoucc",
              "elitessl",
              "comodopciscan",
              "enterprisessl",
              "enterprisepro",
              "enterpriseprowc",
              "pacenterprise",
              "hackerprooftm",
              "hgpcicontrolscan"
            ],
            "EnrollmentFieldMapping": "Organization Name"
          }
        },
        "RegistrationNumber": {
          "FieldData": {
            "RequiredForProducts": [
              "certum"
            ],
            "EnrollmentFieldMapping": "Organization Registration Number"
          }
        },
        "JurisdictionCountry": {
          "FieldData": {
            "RequiredForProducts": [
              "comodoevssl",
              "comodoevcsc",
              "comodoevmdc",
              "sectigoevssl",
              "sectigoevcsc",
              "sectigoevmdc",
              "enterpriseproev",
              "enterpriseproevmdc",
              "positiveevmdc",
              "positiveevssl"
            ],
            "EnrollmentFieldMapping": "Organization Jurisdiction Country"
          }
        },
        "OrganizationAddress": {
          "AddressLine1": {
            "FieldData": {
              "RequiredForProducts": [
                "digi_plus_ssl",
                "digi_wc_ssl",
                "digi_md_ssl",
                "digi_securesite_md",
                "digi_csc",
                "digi_securesite",
                "digi_securesite_pro",
                "digi_securesite_wc",
                "digi_securesite_pro_ev",
                "digi_csc_ev",
                "digi_ev_md_ssl",
                "digi_securesite_ev_md",
                "digi_securesite_ev",
                "digi_ssl_ev_basic",
                "digi_securesite_ev_flex",
                "digi_securesite_pro_ev_flex",
                "digi_securesite_pro_flex",
                "digi_securesite_pro_ev_flex",
                "digi_ssl_basic",
                "digi_securesite_flex",
                "digi_plus_ev_ssl",
                "digi_doc_signing_org_2000",
                "digi_doc_signing_org_5000",
                "digi_truebizid_ev",
                "digi_truebizid",
                "digi_truebizid_ev_md",
                "digi_truebizid_wc",
                "digi_truebizid_md",
                "digi_truebizid_md_wc",
                "digi_truebizid_flex",
                "digi_truebizid_ev_flex",
                "digi_sslwebserver_ev",
                "digi_sslwebserver",
                "digi_sslwebserver_wc",
                "digi_sslwebserver_md_wc",
                "digi_sslwebserver_flex",
                "digi_sslwebserver_ev_flex",
                "truebizid",
                "truebusinessidev",
                "truebusinessidevmd",
                "truebusinessidwildcard",
                "truebizidmdwc",
                "truebizidmd",
                "malwarescan",
                "sslwebserverwildcard",
                "thawtecsc",
                "sslwebserver",
                "sslwebserverev",
                "sectigocsc",
                "sectigoevssl",
                "sectigoevcsc",
                "sectigoevmdc",
                "sectigoovssl",
                "sectigomdc",
                "sectigomdcwildcard",
                "sectigoovwildcard",
                "comodocsc",
                "positiveevssl",
                "comodoevssl",
                "comodoevcsc",
                "enterpriseproev",
                "enterpriseproevmdc",
                "positiveevmdc",
                "comodoevmdc",
                "comodomdc",
                "instantssl",
                "instantsslpro",
                "comodopremiumssl",
                "comodomdcwildcard",
                "comodopremiumwildcard",
                "comodouccwildcard",
                "comodoucc",
                "elitessl",
                "comodopciscan",
                "enterprisessl",
                "enterprisepro",
                "enterpriseprowc",
                "pacenterprise",
                "hackerprooftm",
                "hgpcicontrolscan"
              ],
              "EnrollmentFieldMapping": "Organization Address"
            }
          },
          "Region": {
            "FieldData": {
              "RequiredForProducts": [
                "digi_plus_ssl",
                "digi_wc_ssl",
                "digi_md_ssl",
                "digi_securesite_md",
                "digi_csc",
                "digi_securesite",
                "digi_securesite_pro",
                "digi_securesite_wc",
                "digi_securesite_pro_ev",
                "digi_csc_ev",
                "digi_ev_md_ssl",
                "digi_securesite_ev_md",
                "digi_securesite_ev",
                "digi_ssl_ev_basic",
                "digi_securesite_ev_flex",
                "digi_securesite_pro_ev_flex",
                "digi_securesite_pro_flex",
                "digi_securesite_pro_ev_flex",
                "digi_ssl_basic",
                "digi_securesite_flex",
                "digi_plus_ev_ssl",
                "digi_doc_signing_org_2000",
                "digi_doc_signing_org_5000",
                "digi_truebizid_ev",
                "digi_truebizid",
                "digi_truebizid_ev_md",
                "digi_truebizid_wc",
                "digi_truebizid_md",
                "digi_truebizid_md_wc",
                "digi_truebizid_flex",
                "digi_truebizid_ev_flex",
                "digi_sslwebserver_ev",
                "digi_sslwebserver",
                "digi_sslwebserver_wc",
                "digi_sslwebserver_md_wc",
                "digi_sslwebserver_flex",
                "digi_sslwebserver_ev_flex",
                "truebizid",
                "truebusinessidev",
                "truebusinessidevmd",
                "truebusinessidwildcard",
                "truebizidmdwc",
                "truebizidmd",
                "malwarescan",
                "sslwebserverwildcard",
                "thawtecsc",
                "sslwebserver",
                "sslwebserverev",
                "sectigocsc",
                "sectigoevssl",
                "sectigoevcsc",
                "sectigoevmdc",
                "sectigoovssl",
                "sectigomdc",
                "sectigomdcwildcard",
                "sectigoovwildcard",
                "comodocsc",
                "positiveevssl",
                "comodoevssl",
                "comodoevcsc",
                "enterpriseproev",
                "enterpriseproevmdc",
                "positiveevmdc",
                "comodoevmdc",
                "comodomdc",
                "instantssl",
                "instantsslpro",
                "comodopremiumssl",
                "comodomdcwildcard",
                "comodopremiumwildcard",
                "comodouccwildcard",
                "comodoucc",
                "elitessl",
                "comodopciscan",
                "enterprisessl",
                "enterprisepro",
                "enterpriseprowc",
                "pacenterprise",
                "hackerprooftm",
                "hgpcicontrolscan"
              ],
              "EnrollmentFieldMapping": "Organization State/Province"
            }
          },
          "PostalCode": {
            "FieldData": {
              "RequiredForProducts": [
                "digi_plus_ssl",
                "digi_wc_ssl",
                "digi_md_ssl",
                "digi_securesite_md",
                "digi_csc",
                "digi_securesite",
                "digi_securesite_pro",
                "digi_securesite_wc",
                "digi_securesite_pro_ev",
                "digi_csc_ev",
                "digi_ev_md_ssl",
                "digi_securesite_ev_md",
                "digi_securesite_ev",
                "digi_ssl_ev_basic",
                "digi_securesite_ev_flex",
                "digi_securesite_pro_ev_flex",
                "digi_securesite_pro_flex",
                "digi_securesite_pro_ev_flex",
                "digi_ssl_basic",
                "digi_securesite_flex",
                "digi_plus_ev_ssl",
                "digi_doc_signing_org_2000",
                "digi_doc_signing_org_5000",
                "digi_truebizid_ev",
                "digi_truebizid",
                "digi_truebizid_ev_md",
                "digi_truebizid_wc",
                "digi_truebizid_md",
                "digi_truebizid_md_wc",
                "digi_truebizid_flex",
                "digi_truebizid_ev_flex",
                "digi_sslwebserver_ev",
                "digi_sslwebserver",
                "digi_sslwebserver_wc",
                "digi_sslwebserver_md_wc",
                "digi_sslwebserver_flex",
                "digi_sslwebserver_ev_flex",
                "truebizid",
                "truebusinessidev",
                "truebusinessidevmd",
                "truebusinessidwildcard",
                "truebizidmdwc",
                "truebizidmd",
                "malwarescan",
                "sslwebserverwildcard",
                "thawtecsc",
                "sslwebserver",
                "sslwebserverev",
                "sectigocsc",
                "sectigoevssl",
                "sectigoevcsc",
                "sectigoevmdc",
                "sectigoovssl",
                "sectigomdc",
                "sectigomdcwildcard",
                "sectigoovwildcard",
                "comodocsc",
                "positiveevssl",
                "comodoevssl",
                "comodoevcsc",
                "enterpriseproev",
                "enterpriseproevmdc",
                "positiveevmdc",
                "comodoevmdc",
                "comodomdc",
                "instantssl",
                "instantsslpro",
                "comodopremiumssl",
                "comodomdcwildcard",
                "comodopremiumwildcard",
                "comodouccwildcard",
                "comodoucc",
                "elitessl",
                "comodopciscan",
                "enterprisessl",
                "enterprisepro",
                "enterpriseprowc",
                "pacenterprise",
                "hackerprooftm",
                "hgpcicontrolscan"
              ],
              "EnrollmentFieldMapping": "Organization Postal Code"
            }
          },
          "LocalityName": {
            "FieldData": {
              "RequiredForProducts": [
                "comodocsc",
                "comodoevssl",
                "comodoevcsc",
                "comodoevmdc",
                "comodomdc",
                "comodomdcwildcard",
                "comodouccwildcard",
                "comodoucc",
                "pacenterprise"
              ],
              "EnrollmentFieldMapping": "Organization Locality Name"
            }
          },
          "Country": {
            "FieldData": {
              "RequiredForProducts": [
                "truebizidmd",
                "digi_ev_md_ssl",
                "digi_md_ssl",
                "digi_plus_ev_ssl",
                "digi_plus_ssl",
                "digi_securesite",
                "digi_securesite_wc",
                "digi_securesite_pro_ev",
                "digi_securesite_ev_md",
                "digi_securesite_ev",
                "digi_ssl_ev_basic",
                "digi_securesite_ev_flex",
                "digi_securesite_pro_ev_flex",
                "digi_securesite_pro_flex",
                "digi_ssl_basic",
                "digi_truebizid_ev_flex",
                "digi_sslwebserver_ev_flex",
                "digi_securesite_flex",
                "digi_truebizid_ev",
                "digi_truebizid",
                "digi_truebizid_ev_md",
                "digi_truebizid_wc",
                "digi_truebizid_md",
                "digi_truebizid_md_wc",
                "digi_sslwebserver_ev",
                "digi_sslwebserver",
                "digi_sslwebserver_wc",
                "digi_sslwebserver_md_wc",
                "digi_sslwebserver_flex"
              ],
              "EnrollmentFieldMapping": "Organization Country"
            }
          },
          "Phone": {
            "FieldData": {
              "RequiredForProducts": [
                "truebizidmd",
                "digi_ev_md_ssl",
                "digi_md_ssl",
                "digi_plus_ev_ssl",
                "digi_plus_ssl",
                "digi_securesite",
                "digi_securesite_wc",
                "digi_securesite_pro_ev",
                "digi_securesite_ev_md",
                "digi_securesite_ev",
                "digi_ssl_ev_basic",
                "digi_securesite_ev_flex",
                "digi_securesite_pro_ev_flex",
                "digi_securesite_pro_flex",
                "digi_ssl_basic",
                "digi_sslwebserver_ev_flex",
                "digi_truebizid_ev_flex",
                "digi_securesite_flex",
                "digi_truebizid_ev",
                "digi_truebizid",
                "digi_truebizid_ev_md",
                "digi_truebizid_wc",
                "digi_truebizid_md",
                "digi_truebizid_md_wc",
                "digi_sslwebserver_ev",
                "digi_sslwebserver",
                "digi_sslwebserver_wc",
                "digi_sslwebserver_md_wc",
                "digi_sslwebserver_flex"
              ],
              "EnrollmentFieldMapping": "Organization Phone"
            }
          }
        }
      },
      "ValidityPeriod": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "Validity Period (In Months)"
        }
      },
      "ServerCount": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "Server Count"
        }
      },
      "CSR": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "None"
        }
      },
      "DomainName": {
        "FieldData": {
          "RequiredForProducts": [
            "Certum"
          ],
          "EnrollmentFieldMapping": "Domain Name"
        }
      },
      "WebServerType": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "Web Server Type"
        }
      },
      "DNSNames": {
        "FieldData": {
          "RequiredForProducts": [
            "digi_ssl_ev_basic",
            "digi_ssl_ev_basic-EO",
            "digi_securesite_ev_flex",
            "digi_securesite_ev_flex-EO",
            "digi_securesite_pro_ev_flex",
            "digi_securesite_pro_ev_flex-EO",
            "digi_securesite_pro_flex",
            "digi_securesite_pro_flex-EO",
            "digi_ssl_basic",
            "digi_ssl_basic-EO",
            "digi_securesite_flex",
            "digi_securesite_flex-EO",
            "digi_truebizid_flex",
            "digi_truebizid_flex-EO",
            "digi_truebizid_ev_flex",
            "digi_truebizid_ev_flex-EO",
            "digi_ssl_dv_geotrust_flex",
            "digi_rapidssl",
            "digi_rapidssl_wc",
            "digi_ssl123_flex",
            "digi_sslwebserver_flex",
            "digi_sslwebserver_flex-EO",
            "digi_sslwebserver_ev_flex",
            "digi_sslwebserver_ev_flex-EO",
            "positivemdcssl",
            "positivemdcwildcard",
            "sectigodvucc",
            "sectigouccwildcard",
            "sectigoevmdc",
	    "sectigomdcwildcard",
            "sectigomdc"
          ],
          "EnrollmentFieldMapping": "DNS Names Comma Separated",
          "Array": true
        }
      },
      "isCUOrder": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "Is CU Order?"
        }
      },
      "isRenewalOrder": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "Is Renewal Order?"
        }
      },
      "isTrialOrder": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "Is Trial Order?"
        }
      },
      "AdminContact": {
        "FirstName": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - First Name"
          }
        },
        "LastName": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - Last Name"
          }
        },
        "Phone": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - Phone"
          }
        },
        "Email": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - Email"
          }
        },
        "Title": {
          "FieldData": {
            "RequiredForProducts": [
              "symantec",
              "digi_ssl_ev_basic",
              "digi_sslwebserver_ev_flex",
              "digi_truebizid_ev_flex",
              "digi_securesite_pro_ev_flex",
              "digi_securesite_ev_flex"
            ],
            "EnrollmentFieldMapping": "Admin Contact - Title"
          }
        },
        "OrganizationName": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - Organization Name"
          }
        },
        "AddressLine1": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - Address"
          }
        },
        "City": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - City"
          }
        },
        "Region": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - Region"
          }
        },
        "PostalCode": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - Postal Code"
          }
        },
        "Country": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Admin Contact - Country"
          }
        }
      },
      "TechnicalContact": {
        "FirstName": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - First Name"
          }
        },
        "LastName": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Last Name"
          }
        },
        "SubjectFirstName": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Subject First Name"
          }
        },
        "SubjectLastName": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Subject Last Name"
          }
        },
        "Phone": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Phone"
          }
        },
        "Email": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Email"
          }
        },
        "Title": {
          "FieldData": {
            "RequiredForProducts": [
              "symantec",
              "digi_ssl_ev_basic",
              "digi_securesite_ev_flex",
              "digi_truebizid_ev_flex",
              "digi_sslwebserver_ev_flex"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Title"
          }
        },
        "AddressLine1": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Address"
          }
        },
        "City": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - City"
          }
        },
        "Region": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Region"
          }
        },
        "PostalCode": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Postal Code"
          }
        },
        "Country": {
          "FieldData": {
            "RequiredForProducts": [
              "All"
            ],
            "EnrollmentFieldMapping": "Technical Contact - Country"
          }
        }
      },
      "ApproverEmail": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "Approver Email"
        }
      },
      "FileAuthDVIndicator": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "File Auth Domain Validation"
        }
      },
      "CNAMEAuthDVIndicator": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "CName Auth Domain Validation"
        }
      },
      "SignatureHashAlgorithm": {
        "FieldData": {
          "RequiredForProducts": [
            "All"
          ],
          "EnrollmentFieldMapping": "Signature Hash Algorithm"
        }
      }
    }
  }
```

- *Template Settings - See Configuration Instructional Video on how these work*
```
   "Templates": {
    "positivessl": {
      "ProductID": "positivessl",
      "Parameters": {}
    },
    "positiveevssl": {
      "ProductID": "positiveevssl",
      "Parameters": {}
    },
    "enterpriseproev": {
      "ProductID": "enterpriseproev",
      "Parameters": {}
    },
    "enterpriseproevmdc": {
      "ProductID": "enterpriseproevmdc",
      "Parameters": {}
    },
    "positivesslwildcard": {
      "ProductID": "positivesslwildcard",
      "Parameters": {}
    },
    "positivemdcssl": {
      "ProductID": "positivemdcssl",
      "Parameters": {}
    },
    "positivemdcwildcard": {
      "ProductID": "positivemdcwildcard",
      "Parameters": {}
    },
    "positiveevmdc": {
      "ProductID": "positiveevmdc",
      "Parameters": {}
    },
    "instantssl": {
      "ProductID": "instantssl",
      "Parameters": {}
    },
    "instantsslpro": {
      "ProductID": "instantsslpro",
      "Parameters": {}
    },
    "comodopremiumssl": {
      "ProductID": "comodopremiumssl",
      "Parameters": {}
    },
    "comodopremiumwildcard": {
      "ProductID": "comodopremiumwildcard",
      "Parameters": {}
    },
    "enterprisepro": {
      "ProductID": "enterprisepro",
      "Parameters": {}
    },
    "enterpriseprowc": {
      "ProductID": "enterpriseprowc",
      "Parameters": {}
    },
    "sectigossl": {
      "ProductID": "sectigossl",
      "Parameters": {}
    },
    "sectigodvucc": {
      "ProductID": "sectigodvucc",
      "Parameters": {}
    },
    "sectigoevssl": {
      "ProductID": "sectigoevssl",
      "Parameters": {}
    },
    "sectigoevmdc": {
      "ProductID": "sectigoevmdc",
      "Parameters": {}
    },
    "sectigoovssl": {
      "ProductID": "sectigoovssl",
      "Parameters": {}
    },
    "sectigomdc": {
      "ProductID": "sectigomdc",
      "Parameters": {}
    },
    "sectigomdcwildcard": {
      "ProductID": "sectigomdcwildcard",
      "Parameters": {}
    },
    "sectigoovwildcard": {
      "ProductID": "sectigoovwildcard",
      "Parameters": {}
    },
    "sectigowildcard": {
      "ProductID": "sectigowildcard",
      "Parameters": {}
    },
    "sectigouccwildcard": {
      "ProductID": "sectigouccwildcard",
      "Parameters": {}
    },
    "digi_ssl_ev_basic": {
      "ProductID": "digi_ssl_ev_basic",
      "Parameters": {}
    },
    "digi_ssl_ev_basic-EO": {
      "ProductID": "digi_ssl_ev_basic-EO",
      "Parameters": {}
    },
    "digi_securesite_ev_flex": {
      "ProductID": "digi_securesite_ev_flex",
      "Parameters": {}
    },
    "digi_securesite_ev_flex-EO": {
      "ProductID": "digi_securesite_ev_flex-EO",
      "Parameters": {}
    },
    "digi_securesite_pro_ev_flex": {
      "ProductID": "digi_securesite_pro_ev_flex",
      "Parameters": {}
    },
    "digi_securesite_pro_ev_flex-EO": {
      "ProductID": "digi_securesite_pro_ev_flex-EO",
      "Parameters": {}
    },
    "digi_securesite_pro_flex": {
      "ProductID": "digi_securesite_pro_flex",
      "Parameters": {}
    },
    "digi_securesite_pro_flex-EO": {
      "ProductID": "digi_securesite_pro_flex-EO",
      "Parameters": {}
    },
    "digi_ssl_basic": {
      "ProductID": "digi_ssl_basic",
      "Parameters": {}
    },
    "digi_ssl_basic-EO": {
      "ProductID": "digi_ssl_basic-EO",
      "Parameters": {}
    },
    "digi_securesite_flex": {
      "ProductID": "digi_securesite_flex",
      "Parameters": {}
    },
    "digi_securesite_flex-EO": {
      "ProductID": "digi_securesite_flex-EO",
      "Parameters": {}
    },
    "digi_truebizid_flex": {
      "ProductID": "digi_truebizid_flex",
      "Parameters": {}
    },
    "digi_truebizid_flex-EO": {
      "ProductID": "digi_truebizid_flex-EO",
      "Parameters": {}
    },
    "digi_truebizid_ev_flex": {
      "ProductID": "digi_truebizid_ev_flex",
      "Parameters": {}
    },
    "digi_truebizid_ev_flex-EO": {
      "ProductID": "digi_truebizid_ev_flex-EO",
      "Parameters": {}
    },
    "digi_ssl_dv_geotrust_flex": {
      "ProductID": "digi_ssl_dv_geotrust_flex",
      "Parameters": {}
    },
    "digi_rapidssl": {
      "ProductID": "digi_rapidssl",
      "Parameters": {}
    },
    "digi_rapidssl_wc": {
      "ProductID": "digi_rapidssl_wc",
      "Parameters": {}
    },
    "digi_ssl123_flex": {
      "ProductID": "digi_ssl123_flex",
      "Parameters": {}
    },
    "digi_sslwebserver_flex": {
      "ProductID": "digi_sslwebserver_flex",
      "Parameters": {}
    },
    "digi_sslwebserver_flex-EO": {
      "ProductID": "digi_sslwebserver_flex-EO",
      "Parameters": {}
    },
    "digi_sslwebserver_ev_flex": {
      "ProductID": "digi_sslwebserver_ev_flex",
      "Parameters": {}
    },
    "digi_sslwebserver_ev_flex-EO": {
      "ProductID": "digi_sslwebserver_ev_flex-EO",
      "Parameters": {}
    }
  }
```

- *Gateway Settings*
```
  "CertificateManagers": null,
  "GatewayRegistration": {
    "LogicalName": "SSLStore",
    "GatewayCertificate": {
      "StoreName": "CA",
      "StoreLocation": "LocalMachine",
      "Thumbprint": "339cdd57cfd5b141169b615ff31428782d1da639"
    }
  }
```

- *Service Settings* (Modify these to be in accordance with Keyfactor Standard Gateway Production Settings)
```
  "ServiceSettings": {
    "ViewIdleMinutes": 1,
    "FullScanPeriodHours": 1,
    "PartialScanPeriodMinutes": 1
  }
```

5) Gateway Server - Save the newly modified config.json to the following location "C:\Program Files\Keyfactor\Keyfactor AnyGateway"

### Template Installation

1) Command Server - Copy and Unzip the Template Setup Files located [Here](https://github.com/Keyfactor/sslstore-cagateway/raw/main/TemplateSetup.zip)
2) Command Server - Change the Security Settings in the CaTemplateUserSecurity.csv file to the appropriate settings for Test or Production
3) Command Server - Run the CreateTemplate.ps1 file and choose option 1 to create the templates in active directory.
   *Note if you get errors the security is likely wrong and you will have to add the security manually according to Keyfactor standards* 
4) Command Server - Use the Keyfactor Portal to Import the Templates created in Active Directory in step #3 above
5) Command Server - Run the CreateTemplate.ps1 file and choose option 3 to create all the enrollment fields.  
   *Note You will have to override the default API Questions to the appropriate information.*

### Certificate Authority Installation
1) Gateway Server - Start the Keyfactor Gateway Service
2) Run the set Gateway command similar to below
```ps
Set-KeyfactorGatewayConfig -LogicalName "SSLStore" -FilePath [path to json file] -PublishAd
```
3) Command Server - Import the certificate authority in Keyfactor Portal 

***

### License
[Apache](https://apache.org/licenses/LICENSE-2.0)

