# VS Cloud Kernel Sample Services

## Projects

Docker containers:

- `containers/Sample.WebSite` a confidential client that signs in to the Application and invokes the `Sample.WebApi` middle tier
- `containers/Sample.WebApi` the middle-tier api called by the client, requires a valid auth token
- `containers/Sample.DataAccess` the back-end api, not public, called by the `Sample.WebApi`

Libraries:

- `lib/Sample.Hosting` a shared library for generic startup support

## Authentication

Service-to-service authentication uses the SAL libraries.

>[Server-to-server authentication (AAD Docs)](https://identitydocs.azurewebsites.net/static/overview/server-to-server.html?q=SAL) list a variety of authentication modes that an app can choose from. This sample implement "Protected Forward Tokens (PFT)" using the SAL library.  
>[Server-to-Server Authorization Library (SAL)](https://msgo.azurewebsites.net/add/develop/auth/server-to-server-authentication.html#repo)  
>[Microsoft-IdentityModel-S2S (Repo)](https://identitydivision.visualstudio.com/DevEx/_git/Microsoft-IdentityModel-S2S) contains the sources and sample code for using SAL. See "samples/POPUsingPolicies".  
>[DevDiv-OnlineServices IDDP packages](https://dev.azure.com/devdiv/OnlineServices/_packaging?_a=feed&feed=vsclk-identitydivision-iddp) is a SAL package feed hosted by devdiv-OnlineServices. The [original feed](https://identitydivision.visualstudio.com/DevEx/_packaging?_a=feed&feed=IDDP%40Local) isnt' yet available as an Upstream Source.  

### Authentication in Sample.WebSite

TODO: Web-based sign in to VisualStudioCloudKernel Application.

Routes:

```http
    GET /        (unauthenticated)
    GET /profile (authenticated)
    GET /profile (unauthenticated) --> /login
    GET /login --> AAD
    GET /logout --> /
```

### Authentication in Sample.WebApi

TODO: JWT validation for VisualStudioCloudKernel Application. Token exchange to invoke back-ends OBO authenticated user.

Routes:

```http
```

### Authentication in Sample.DataAccess

TODO: JWT validation for VisualStudioCloudKernel Application.

Routes:

```http
```