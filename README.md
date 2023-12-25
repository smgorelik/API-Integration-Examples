# Popular IT Security Vendor API Integration Examples
This repository specializes in API integrations with prominent IT platforms, featuring C#-based implementations for a variety of popular API requests. A key focus is on functionalities like listing protected devices. The repository serves as a practical reference for developers seeking to integrate with these platforms, offering C# examples for essential API calls.

## Microsoft Azure and EDR API Authentication
For API authentication without user interaction, a secret token must be generated. This token allows the application to retrieve data and must be configured with the appropriate API permission definitions. For instance, to retrieve installed devices, the token needs permissions like Application - Device.Read.All. It is the user's responsibility to regenerate tokens upon expiry, with a maximum validity period of up to 6 months. The provided implementation code also includes a feature to verify the remaining validity period of the token.

![Token Generation Process](https://github.com/smgorelik/API-Integration-Examples/blob/main/MicrosoftGraph/2023-12-24%2017_23_35-Graph%20Python%20quick%20start%20-%20Microsoft%20Entra%20admin%20center.png)
![API Permission](https://github.com/smgorelik/API-Integration-Examples/blob/main/MicrosoftGraph/2023-12-24%2017_25_42-Graph%20Python%20quick%20start%20-%20Microsoft%20Entra%20admin%20center.png)

### Developers utilizing this code must define their secret token and other necessary credentials in the Secrets.config XML file. This file should include entries for tenantId, clientId, SecretId, and SecretValue under appSettings

## Integration with Qualys VMP
Qualys VMP, a renowned vendor for vulnerability prioritization, differs in its approach to API integration. Unlike vendors that use API tokens, Qualys VMP requires the provision of a username, password, and server URL for integration. These credentials should be securely stored in the Secrets.config XML file
### Ensure that these credentials are correctly configured to establish a successful connection with Qualys 

## SentinelOne Integration
SentinelOne, a leading EDR (Endpoint Detection and Response) vendor, enables API integration through the generation of a user-specific token. Each user can generate one token, used for authenticating REST API requests.
Our examples focus on retrieving device and application information for a specific site using SentinelOne's API. This process requires the use of the generated API token for secure access.


Configuring the Secrets.config File
After obtaining your API token, configure it along with the Management URL in the Secrets.config XML file
