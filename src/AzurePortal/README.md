
# Azure portal extension
Azure portal extension for the VSOnline/plans resource.

## Getting Started
Follow the steps below to get started.
Full detail available at [PortalDocs](https://github.com/Azure/portaldocs/blob/master/portal-sdk/generated/top-ap-cli.md#cli-overview)

### PreReq: One time configuration steps
 - Install the LTS of nodejs [download](https://nodejs.org/en/download)
 - Install the .NET 4.7.2 *Developer Pack* - [located here](https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net472-developer-pack-offline-installer)
 - NuGet Credential provider
    1. Connect to the AzurePortal Feed https://msazure.visualstudio.com/One/_packaging?_a=connect&feed=AzurePortal
    1. Select NuGet.exe under the NuGet header
    1. Click the 'Get the tools' button in the top right corner
    1. Follow steps 1 and 2 to download the latest NuGet version and the credential provider.

 - NPM Auth Personal Access Token (PAT)

    Just as NuGet needed the credentidal provider npm requires a PAT for auth.  Which can be configured as follows.

    1. Connect to the AzurePortal Feed https://msazure.visualstudio.com/One/_packaging?_a=connect&feed=AzurePortal
    1. Select npm under the npm header
    1. Click the 'Get the tools' button in the top right corner
    1. Optional. Follow steps 1 to install node.js and npm if not already done so.
    1. Follow step 2 to install vsts-npm-auth node.
    1. Add a .npmrc file to your project or empty directory with the following content
        ```
        registry=https://msazure.pkgs.visualstudio.com/_packaging/AzurePortal/npm/registry/
        always-auth=true
        ```
    1. From the command prompt in the same directory:
        - set the default npm registry
           ```
           npm config set registry https://msazure.pkgs.visualstudio.com/_packaging/AzurePortal/npm/registry/
           ```
        - generate a readonly PAT using vsts-npm-auth
           ```
           vsts-npm-auth -R -config .npmrc
           ```
           Generally the PAT will be written to %USERPROFILE%\.npmrc we strongly recommend not checking your PAT into source control; anyone with access to your PAT can interact with Azure DevOps Services as you.

 - Path
    Ensure the following are on your path i.e resolve when typed in and run from the command prompt.
    1. `NuGet` and the above installed credential provider is on your path.
    1. Ensure `npm` is on your path.
    1. Ensure `msbuild` is on your path.

    If not present you can add the items to your path as follows:
    1. WindowsKey + R
    1. `rundll32.exe sysdm.cpl,EditEnvironmentVariables`
    1. In the dialog click `Edit` on the `Path` variable and add (note paths may vary depending on your environment and msbuiuld version)
        - for npm `C:\Users\youralias\AppData\Roaming\npm`
        - for nuget and cred provider `C:\Users\youralias\.nuget`
        - for msbuild `C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin`

If you have run into problems checkout the [Video:One time configuration steps](https://msit.microsoftstream.com/video/d1f15784-da81-4354-933d-51e517d38cc1?st=657)

<a name="cli-overview-setup-and-installation-installing-the-azure-portal-extension-developer-cli"></a>
### Installing the Azure portal extension developer CLI

With the one time configuration steps complete you can now install the CLI as you would with any other node module.

1. Run the following command in the directory that contains your .npmrc.
    ```
    npm install -g @microsoft/azureportalcli
    ```
### Run the project

1. Run command prompt/powershell as Admin
1. Open your browser, leave at least 1 tab open (otherwise chrome extensions may have trouble loading up)
1. Run the following command, after successful build, a browser tab should pop-up. Done!
```
cd C:\mywork\vsclk-core\src\AzurePortal\src\Default\Extension
npm start
```

