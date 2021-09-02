# {{ name }}
## {{ integration_type | capitalize }}

{{ description }}

<!-- add integration specific information below -->
*** 
## **Configuration**

**Overview**

AWS Certificate Manager is a service that lets you easily provision, manage, and deploy public and private Secure Sockets Layer/Transport Layer Security (SSL/TLS) certificates for use with AWS services and your internal connected resources. SSL/TLS certificates are used to secure network communications and establish the identity of websites over the Internet as well as resources on private networks. AWS Certificate Manager removes the time-consuming manual process of purchasing, uploading, and renewing SSL/TLS certificates.

### Documentation

- [Cert Manager API](https://docs.aws.amazon.com/acm/latest/userguide/sdk.html)
- [Aws Region Codes](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.RegionsAndAvailabilityZones.html)

### Supported Functionality
- Add/Delete/Replace Root Certificates
- Add/Delete/Replace Certificates with Public and Private Keys
- Inventory Root Certificates
- Inventory Certificates with Public and Private Keys

### Assumptions:
- In order for the Certificates and Keys to renew or reenroll correctly, they need to derive of the <alias> which is passed into the any agent.  The <alias> drives the files and object creation and is essentially how we are able to relate them to each other.

### Not Implemented/Supported
- Reenrollment, Management, Discovery

## **Installation**

### Cert Store Type Settings
![image.png](/Media/Images/CertStoreTypes.gif)

### Important Items
1)  **Short Name** must Match was is in the screenshot "AWSCerMan"

2) **Regions** should be defined like they are in the screenshot under Store Path Value.  There is a list of regions here:
https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.RegionsAndAvailabilityZones.html
We define them as "US East 2" in the UI and convert them to the amazon format in the code which is "us-east-2" just to make them look better in the UI

### Cert Store Settings
![image.png](/Media/Images/CertStoreSettings.gif)

1) **Client Machine** will be the AWS Account Now then click "Change Credentials" to enter the AWS Access Key and Secret Access Key
2) **Store Path** will be the Region that you can select from the dropdown defined in the Cert Store Type

### Cert Store Credentials
![image.png](/Media/Images/ChangeCredentials.gif)

1) **User** - Will be where the AWS Access Key ID goes
2) **Password** -  Will be where the Secret Access Key goes.
