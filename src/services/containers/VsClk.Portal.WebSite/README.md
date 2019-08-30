# VSOnline Portal

Tha application itself is Asp.Net Core Application that serves a React SPA (built using [Create React Application](https://github.com/facebook/create-react-app)).

## Development

In development mode the portal works as a proxy to the CRA dev server with hot reloading and watcher.

Locally, the portal has to run on port `3000` and the CRA dev server is set up to run on port 3030. Since we are using service workers, browsers require the app to use HTTPS. Dotnet makes this easier to manage - to enable trusted certificates run (it will probably want your password):
```sh
dotnet dev-certs https --trust
```

CORS headers on environment registration enable requests from `https://localhost:3000`.


VSCode has the tasks set up when you open folder `<projectRoot>/src/services/containers/VsClk.Portal.WebSite`

Create secrets file `<projectRoot>/src/services/containers/VsClk.Portal.WebSite/appsettings.secrets.json` with content:

```json
{
  "AppSettings": {
    "IsLocal": true,
    "EnvironmentRegistrationEndpoint": "https://online.dev.core.vsengsaas.visualstudio.com/api/v1/environments"
  }
}
```


- There's .Net Core Launch (web) configuration that starts the app on port 3000. You still have to go and start the CRA server.
- There's a build task that starts dev server SPA or:
```sh
cd ClientApp
yarn start
```

> You might need to install the dependencies for the React App - please use yarn.

For local development, the application is set up to talk to local Environment Registration service on http://localhost:5000.

To start the Environment registration service, you can use launch scripts in the project root folder.