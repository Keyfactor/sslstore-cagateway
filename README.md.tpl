# {{ name }}
## {{ integration_type | capitalize }}

{{ description }}

<!-- add integration specific information below -->
*** 
# Getting Started

## Integration Limitations

1) The Snyc process will only Sync Certificates that have been enrolled via Keyfactor.  There is no Quovadis API call to get a list of all the certificates in Quovadis so we can't sync Certs that came outside of Keyfactor.

2) P12 Certificates can only be enrolled and not Renewed or Revoked since those are not available for download via the Quovadis API.

3) Certificates can only be downloaded 25 times from Quovadis so the Sync will grab it the first time and then will not try again to avoid reaching the limit.

## Internal Keyfactor Videos
There are two videos that show the configuration and functionality of the integration.  These can be made available externally upon request.

[Config Video](https://web.microsoftstream.com/video/c7c59d62-74e6-4889-a13e-3a372749c211)

[Functionality Video](https://web.microsoftstream.com/video/afd315e1-c90c-4e1c-a64d-473b9c785f1a)

## Standard Gateway Installation
To begin, you must have the CA Gateway Service 21.3.2 installed and operational before attempting to configure the Quovadis Any Gateway plugin. This integration was tested with Keyfactor 8.7.0.0.
To install the gateway follow these instructions.

1) Gateway Server - run the installation .msi located [Here](https://github.com/Keyfactor/quovadis-cagateway/raw/main/AnyGateway-21.3.2.msi)

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


## Quovadis AnyGateway Specific Configuration
It is important to note that importing the  Quovadis configuration into the CA Gateway prior to installing the binaries must be completed. Additionally, the CA Gateway service
must be running in order to succesfully import the configuation. When the CA Gateway service starts it will attempt to validate the connection information to 
the CA.  Without the imported configuration, the service will fail to start.

### Binary Installation

1) Get the Latest Zip File from [Here](https://github.com/Keyfactor/quovadis-cagateway/releases)
2) Gateway Server - Copy the QuovadisCAProxy.dll to the location where the Gateway Framework was installed (usually C:\Program Files\Keyfactor\Keyfactor AnyGateway)

### Configuration Changes
1) Gateway Server - Edit the CAProxyServer.exe.config file and replace the line that says "NoOp" with the line below:
   ```
   <alias alias="CAConnector" type="Keyfactor.AnyGateway.Quovadis.QuovadisCaProxy, QuovadisCaProxy"/>
   ```
2) Gateway Server - Install the Root Quovadis Certificate that was received from Quovadis [Here](https://github.com/Keyfactor/quovadis-cagateway/raw/main/Quovadis%20Root%20Cert.cer)

3) Gateway Server - Install the Intermediate Quovadis Certificate that was received from Quovadis [Here](https://github.com/Keyfactor/quovadis-cagateway/raw/main/Quovadis%20Intermediate.cer)

4) Gateway Server - Take the sample Config.json located [Here](https://github.com/Keyfactor/quovadis-cagateway/raw/main/SampleConfig.json) and make the following modifications

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
- *Quovadis Environment Settings* (Modify these with the keys and Urls obtained from Quovadis)
```
  "CAConnection": {
    "BaseUrl": "https://tlclientdev.quovadisglobal.com/ws/CertificateServices.asmx",
    "WebServiceSigningCertDir": "C:\\Program Files\\Keyfactor\\Keyfactor AnyGateway\\SomeFileYouGot.p12",
    "WebServiceSigningCertPassword": "SomePasswordYouSet",
    "OrganizationId":"KeyFactor",
    "SMIME P12 no registrant interaction":"NoSync",
    "QV Business SSL 10 SAN - Admin generate":"Sync",
    "SMIME Client Side Key Gen":"NoSync",
    "QV Business SSL 10 SAN - Subscriber Interaction":"Sync",
    "KeyfactorApiUrl":"https://someKeyfactorDomain/KeyfactorAPI/",
    "KeyfactorApiUserId":"KFApiUserId",
    "KeyfactorApiPassword":"KFAPIPassword"
  }
```

- *Template Settings - See Configuration Instructional Video on how these work*
```
  "Templates": {
    "SMIME P12 no registrant interaction": {
      "ProductID": "SMIME P12 no registrant interaction",
      "Parameters": {
        "CanSync":"false",
        "ProductName": "SMIME P12 no registrant interaction",
        "EnrollmentTemplate": "<InitiateInviteRequest><ValidityPeriod>1</ValidityPeriod><DateTime>DateTime.Now<\/DateTime><AdministratorEmailAddress>Enrollment|Admin Email<\/AdministratorEmailAddress><TemplateId>2166<\/TemplateId><CertContentFields><CN>CSR|CN<\/CN><C>CSR|C<\/C><O>CSR|O<\/O><OU><Field>CSR|OU<\/Field><\/OU><\/CertContentFields><RegistrantInfo><Password>Enrollment|Password<\/Password><ConfirmPassword>Enrollment|Password<\/ConfirmPassword><\/RegistrantInfo><Account><Name>KeyFactor<\/Name><Organisation>KeyFactor<\/Organisation><\/Account><\/InitiateInviteRequest>"
      }
    },
    "QV Business SSL 10 SAN - Subscriber Interaction": {
      "ProductID": "QV Business SSL 10 SAN - Subscriber Interaction",
      "Parameters": {
        "CanSync":"true",
        "ProductName": "QV Business SSL 10 SAN - Subscriber Interaction",
        "EnrollmentTemplate": "<RequestSSLCertRequest><DateTime>DateTime.Now</DateTime>\t<SubscriberEmailAddress>Enrollment|Subscriber Email</SubscriberEmailAddress>\t<CertFields>\t\t<CN>CSR|CN</CN>\t\t<O>CSR|O</O>\t\t<OU>\t\t\t<Field>CSR|OU</Field>\t\t</OU>\t\t<SAN>\t\t\t<Field>\t\t\t\t<Type>DnsName</Type>\t\t\t\t<Value>CSR|CN</Value>\t\t\t</Field>\t\t</SAN>\t</CertFields>\t<CSR>CSR|Raw</CSR>\t<CertificateType>2150</CertificateType>\t<Account>\t\t<Name>KeyFactor</Name>\t\t<Organisation>KeyFactor</Organisation>\t</Account>\t<ServerPlatform>Microsoft IIS7</ServerPlatform></RequestSSLCertRequest>"
      }
    },
    "QV Business SSL 10 SAN - Admin generate": {
      "ProductID": "QV Business SSL 10 SAN - Admin generate",
      "Parameters": {
        "CanSync":"true",
        "ProductName": "QV Business SSL 10 SAN - Admin generate",
        "EnrollmentTemplate": "<RequestSSLCertRequest><DateTime>DateTime.Now</DateTime>\t<SubscriberEmailAddress>Enrollment|Subscriber Email</SubscriberEmailAddress>\t<CertFields>\t\t<CN>CSR|CN</CN>\t\t<O>CSR|O</O>\t\t<OU>\t\t\t<Field>CSR|OU</Field>\t\t</OU>\t\t<SAN>\t\t\t<Field>\t\t\t\t<Type>DnsName</Type>\t\t\t\t<Value>CSR|CN</Value>\t\t\t</Field>\t\t</SAN>\t</CertFields>\t\t<CertificateType>2151</CertificateType>\t<CSR>CSR|Raw</CSR>\t<Account>\t<Name>KeyFactor</Name>\t\t<Organisation>KeyFactor</Organisation>\t</Account>\t<ServerPlatform>Microsoft IIS7</ServerPlatform></RequestSSLCertRequest>"
      }
    },
    "SMIME Client Side Key Gen": {
      "ProductID": "SMIME Client Side Key Gen",
      "Parameters": {
        "CanSync":"false",
        "ProductName": "SMIME Client Side Key Gen",
        "EnrollmentTemplate": "<InitiateInviteRequest><ValidityPeriod>1</ValidityPeriod><Country>Enrollment|Country<\/Country><DateTime>DateTime.Now<\/DateTime><AdministratorEmailAddress>Enrollment|Admin Email<\/AdministratorEmailAddress><TemplateId>2166<\/TemplateId><CertContentFields><CN>CSR|CN<\/CN><C>CSR|C<\/C><O>CSR|O<\/O><OU><Field>CSR|OU<\/Field><\/OU><\/CertContentFields><RegistrantInfo><Password>Enrollment|Password<\/Password><ConfirmPassword>Enrollment|Password<\/ConfirmPassword><\/RegistrantInfo><Account><Name>KeyFactor<\/Name><Organisation>KeyFactor<\/Organisation><\/Account><\/InitiateInviteRequest>"
      }
    }
  }
```

- *Gateway Settings*
```
  "CertificateManagers": null,
  "GatewayRegistration": {
    "LogicalName": "Quovadis",
    "GatewayCertificate": {
      "StoreName": "CA",
      "StoreLocation": "LocalMachine",
      "Thumbprint": "fac5470a60d04b90ce947ebbf11b5f3fe9b275ae"
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

1) Command Server - Copy and Unzip the Template Setup Files located [Here](https://github.com/Keyfactor/Quovadis-AnyGateway/raw/main/TemplateSetup.zip)
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
Set-KeyfactorGatewayConfig -LogicalName "Quovadis" -FilePath [path to json file] -PublishAd
```
3) Command Server - Import the certificate authority in Keyfactor Portal 

***

### License
[Apache](https://apache.org/licenses/LICENSE-2.0)
