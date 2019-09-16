# VSOnline Portal

Tha application itself is Asp.Net Core Application that serves a React SPA (built using [Create React Application](https://github.com/facebook/create-react-app)).

## Development

In development mode the portal works as a proxy to the CRA dev server with hot reloading and watcher.

Locally, the portal has to run on port `3000` and the CRA dev server is set up to run on port 3030. Since we are using service workers, browsers require the app to use HTTPS. Dotnet makes this easier to manage - to enable trusted certificates run (it will probably want your password):
```sh
dotnet dev-certs https --trust
```

CORS headers on environment registration enable requests from `https://localhost:3000`.

When working only on the react side of the project you can start from `<projectRoot>/src/services/containers/VsClk.Portal.WebSite/ClientApp`

### Set Up

Required Software:
- Node.js (v10.15.3) (https://nodejs.org/en/)
- Yarn (https://yarnpkg.com/lang/en/)
- .NET Core (https://dotnet.microsoft.com/download/dotnet-core/2.2)
- (https://github.com/Microsoft/artifacts-credprovider#readmecd%20)

1. [From project root] Restore dotnet dependencies (`dotnet restore <projectRoot>/dirs.proj --interactive`)
1. [From ClientApp] Install node dependencies (`yarn`)
1. [From ClientApp] Start the portal (`yarn start`)

### Debugging .Net Portal 

VSCode has the tasks set up when you open folder `<projectRoot>/src/services/containers/VsClk.Portal.WebSite`

- There's .Net Core Launch (web) configuration that starts the app on port 3000. You still have to go and start the CRA server.

```sh
cd ClientApp
yarn watch:client
```

