# First-Party Applicaiton Registrations

First-Party appids are registered via [AAD Onboarding Portal](https://firstpartyportal.msidentity.com). Documentation is at [AAD Onboarding | Identity Docs](https://identitydocs.azurewebsites.net/static/aad/index.html).

New registrations require approval from the AAD team. Changes to existing registrations may require approval from AAD, or from the appid's owner, which should be [Visual Studio Cloud Kernel App Admins](mailto:vsclk-app-admins.microsoft.com). This security group group is maintained under https://idweb.

New registrations are provisioined in the PPE environemnt (tenant). They are useful for testing, but not for production. Once tested, PPE registrations must be promoted to PROD via the registration portal.

| Name | Appid | Token Type | Environment |
| --- | --- | --- | --- |
| [Visual Studio Services API](https://firstpartyportal.msidentity.com/applicationDetails/GetApplicationDetails?appId=9bd5ab7f-4031-4045-ace9-6bebbad202f6&environment=PPE#requestStatus) | 9bd5ab7f-4031-4045-ace9-6bebbad202f6 | v2 | PPE |
| [Visual Studio Services Native Client](https://firstpartyportal.msidentity.com/applicationDetails/GetApplicationDetails?appId=4b12dcbf-acd2-40cc-911d-3a18fdf147ce&environment=PPE#requestStatus) | 4b12dcbf-acd2-40cc-911d-3a18fdf147ce | v2 | PPE |
| Visual Studio Services Web Client [SPA] | _Not yet allocated. Same as API?_ | v2 | PPE |

## Visual Studio Services API

Used for all public **web api endpoints**, including Live Share, IntelliCode, Cloud Environments, Presence, etc.

Resource Scopes:

- `api://9bd5ab7f-4031-4045-ace9-6bebbad202f6/all`

## Visual Studio Services Native Client

Used from **public native clients** such as the VS Code extension.

## Visual Studio Services Web Client

Used from **confidential web clients** including the portal web site and its constintuent SPA. _It is possible that we will reuse the "API" registration for this purpose, collapsing the three appids to two._

## Usage Cases and Sample Code

_TODO: fill these out with details once we get something working. ADAL, MSAL, pure JS..._

### Sign-in from Native Client

### Invoke API from Native Client

### Sign-in from Web Client

### Invoke API from Web Client

### Invoke API from SPA Client

### Validate JWT from Web API
