# Codespaces Web Portal

## Getting Started

### Prerequisites

- Connect to npm feed
  1. Open [NodeRepos](https://devdiv.visualstudio.com/DefaultCollection/OnlineServices/_packaging?_a=feed&feed=NodeRepos)
  2. Click on 'Connect to feed', Select npm
      - Append feed to local machine's .npmrc (%userprofile%\.npmrc) file
      - Run the auth helper commands

*Note: on macOS since there are no helpers, make sure your `~/.npmrc` has all the "auth blocs" required. You can find the auth blocs after connecting to the NPM feed, under other. You will need a DevOps personal access token.  
You should have one "`; begin/end auth token`" for each of the following packages:*
```
//devdiv.pkgs.visualstudio.com/_packaging/NodeRepos/npm/registry/:username=devdiv
//devdiv.pkgs.visualstudio.com/DevDiv/_packaging/playwright/npm/registry/:username=devdiv
//devdiv.pkgs.visualstudio.com/_packaging/VS/npm/registry/:username=devdiv
//devdiv.pkgs.visualstudio.com/_packaging/Cascade/npm/registry/:username=devdiv
//devdiv.pkgs.visualstudio.com/DevDiv/_packaging/playwright/npm/registry/:username=devdiv
```
🏥 To convert your personal access token to base64 you can run `echo -n 'myToken' | python -m base64`

⚠️ It's possible you get 401 unauthorized access from time to time. If that happens try re-generating your personal access token and updating your `~/.npmrc`

### Required Software

- [Node.js](https://nodejs.org/en/)
- [Yarn](https://yarnpkg.com/lang/en/)
- [.NET Core](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [Azure Artifacts Credential Provider](https://github.com/Microsoft/artifacts-credprovider#readmecd%20)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
- [C# extension for VS Code](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- [Docker Desktop](https://www.docker.com/get-started)

### Build & Run

1. Change directory to the root of the repository
```
cd vsclk-core
```

2. Restore dotnet dependencies
```
dotnet restore dirs.proj --interactive
```

3. Change directory to the root of `Website`
```
cd src/Portal/PortalWebsite/Src/Website
```

4. Install all dependencies
```
yarn && yarn setup
```

5. Login with the `azure cli`
```
az login
```

6. Setup the local DNS & nginx server

> This only needs to happen the first time. After that, the setting should be set in system settings, and `yarn start` should be sufficient.

Windows (from an Administrator prompt)
```
yarn start:dns
```

macOS
```
sudo yarn start:dns
```

7. Flush your DNS

Windows
```
ipconfig /flushdns
```

macOS
```
sudo killall -HUP mDNSResponder; sleep 2;
```

8. Start the portal
```
yarn start
```

9. Open https://online.dev.core.vsengsaas.visualstudio.com in the browser. You should see a 🚧 in the top bar if the portal is running locally.

## Pointing Local Portal to Your Service Devstamp

Set `API_ORIGIN` variable in `src/Portal/PortalWebsite/Src/dev-local.env` to your NGrok URL. When you start the portal after that, the local NGinx will route API calls to your devstamp instead of dev.

## Tip & Tricks

When looking at debug logs in chrome, you can exclude the common & browser files from stack traces by marking them as library code.

## Playwright automation

**npx playwright-web**
This will lauch the recorder in the web with localhost:8080 port number by default.

- to process the json that is saved from playwright recorder before checking it into the repo, 
**npm run test:ui-prep-json -- --env=dev --user=<emailaddress> --password=<value> --file="C:\Users\*****\OneDrive - Microsoft\Desktop\temp\vso-sanity.json"** 
 This command will take the json and removes the user id, password & url info in it and place it under your temp directory test-prep folder.
**NOTE:** please make sure to have "" quotes, if there is a space in this path. 

- in order to perform test in the local machine the command is
   **npm run test:ui -- --env=local --user=<emailaddress> --password=<value>** to perform the test JSON files synchronously inside the src/portal/test/actions directory.
   They are executed in debug mode, you will be able to see them lauching the browser.

- if we want to test a particular json file and the command for running in headless mode, then run the following command with the unmodified json file from the playwright recorder.
  **npx playwright-cli --verbose "C:\Users\*****\OneDrive - Microsoft\Desktop\temp\vso-sanity.json"**

- if we want to run it in debug mode then run the following command with the unmodified json file that we have got from the recorder.
 **npx playwright-cli --debug --verbose "C:\Users\*****\OneDrive - Microsoft\Desktop\temp\vso-sanity.json"**

