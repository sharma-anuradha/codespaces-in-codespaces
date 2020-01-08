# VSOnline Portal

Tha application itself is Asp.Net Core Application that serves a React SPA (built using [Create React Application](https://github.com/facebook/create-react-app)).

## Development

In development mode the portal works as a proxy to the CRA dev server with hot reloading and watcher.

Locally, the portal has to run on port `443` and the CRA dev server is set up to run on port 3030. Since we are using service workers, browsers require the app to use HTTPS. Use `Get Dev certificate` to set up the SSL cert locally.

CORS headers on environment registration enable requests from `https://localhost:443`.

When working only on the react side of the project you can start from `<projectRoot>/src/services/containers/VsClk.Portal.WebSite/ClientApp`

### Set Up

Prerequisite:
- Connect to npm feed
  1. Open [NodeRepos](https://devdiv.visualstudio.com/DefaultCollection/OnlineServices/_packaging?_a=feed&feed=NodeRepos)
  2. Click on 'Connect to feed', Select npm
      - Append feed to local machine's .npmrc (%userprofile%\.npmrc) file
      - Run the auth helper commands

Required Software:
- Node.js (v10.15.3) (https://nodejs.org/en/)
- Yarn (https://yarnpkg.com/lang/en/)
- .NET Core (https://dotnet.microsoft.com/download/dotnet-core/3.1)
- (https://github.com/Microsoft/artifacts-credprovider#readmecd%20)
- Azure CLI (https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
- C# extension for VS Code (https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)

1. [From project root] Restore dotnet dependencies (`dotnet restore <projectRoot>/dirs.proj --interactive`)
2. [From ClientApp] Install node dependencies (`yarn`)
3. [From ClientApp] Get Dev certificate (`yarn get-dev-cert`). Note that you need to be authenticated to `azure cli` for this. (`az login`). Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest
4. Uncomment the line `127.0.0.1 localhost` in your `hosts` file and replace `localhost` with `online.dev.core.vsengsaas.visualstudio.com`. (https://support.rackspace.com/how-to/modify-your-hosts-file/)  
5. Run `ipconfig /flushdns` from the command line
6. [From ClientApp] Start the portal (`[sudo] yarn start`)
7. Go to `online.dev.core.vsengsaas.visualstudio.com` in the browser

#### Debugging TypeScript

Open browser dev tools, search for .tsx files in sources, and set breakpoints there.

#### Debugging C#

Instead of `yarn start`, run `yarn start:webpack-dev-server`. Visit `online.dev.core.vsengsaas.visualstudio.com` in the browser as before. Then you can set breakpoints in the C# files in your editor.

### Debugging .Net Portal 

VSCode has the tasks set up when you open folder `<projectRoot>/src/services/containers/VsClk.Portal.WebSite`

- There's .Net Core Launch (web) configuration that starts the app on port 3000. You still have to go and start the CRA server.

```sh
cd ClientApp
yarn watch:client
```

### GitHub Auth working locally.

Run `yarn update-github-secret` from ClientApp folder.
> You have to be logged in with azure cli for the script to work.

Or manually:

You need to get `Local-Config-GitHubAppClientSecret` from our dev azure keyvault (`vsclk-online-dev-kv` in `vsclk-core-dev` subscription) into `appsettings.secrets.json` file in Portal.

Azure Cli:
```sh
az keyvault secret show --name "Local-Config-GitHubAppClientSecret" --vault-name "vsclk-online-dev-kv" --sub "vsclk-core-dev"
```

Once you're done, the `appsettings.secrets.json` file should look something like this.

```json
{
  "AppSettings": {
    "GitHubAppClientSecret":"460b..."
  }
}
```

# Running port forwarding locally
In ClientApp folder:

As a setup step you need to export a certificate.

```
dotnet dev-certs https --export-path cert.pfx --password pass --trust
```

Run `VSO_PF_SESSION_ID=<<YourSessionID>> yarn start:pf-proxy` on mac

Or on windows
``` cmd
set VSO_PF_SESSION_ID=<<YourSessionID>>
yarn start:pf-proxy
```

Set up your port forwarding target in URL utils `ClientApp/src/common/url-utils.ts`

There's `dev_PortForwardingOverride` function to create the routing details you need.

1. Change sessionId and port in that function to target the server you want to port forward.
2. Uncomment `// dev_PortForwardingOverride(originalUrl) ||` line in `getRoutingDetails` function

Look for `// <<- DEV PortForwarding here` comments.

# Fixing component governance security issues
If the project is using a package with security issue, we will get alerts in [VSTS component governance](https://devdiv.visualstudio.com/DefaultCollection/OnlineServices/_componentGovernance/vsclk-core?_a=alerts&typeId=1981470&alerts-view-option=active).

To fix the issues, navigate to `ClientApp/`, then run command
`yarn audit` and upgrade any outdated packages accordingly.