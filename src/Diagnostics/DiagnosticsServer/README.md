# VS Codespaces Diagnostics

## Setup Requirements

- .NET Core 3.1
- Node
  - The install scripts (`npm install`, `npm run build:dev`) are set up to run on `build` so you shouldn't need to call it yourself from CLI.
- In your `CEDev/AppSettings.json` file, in `AppSettings`, set `"redirectStandardOutToLogsDirectory": true` 

## How to Run

- Multi-Deploy
  - If you already use VS, VSMac, or VSCode, you can add the `DianosticsServer` as an additional startup project to deploy alongside it, and close when you stop your debugging processes.
- Single Deployment
  - You can also run the Front/BackEnd API services on their own, and use either an alternative editor (Like VSCode) or manually call `dotnet run` and keep this service running all the time. When you redeploy the other services, the hooks built into the Diagnostics Server should automatically pick up the new logs.




