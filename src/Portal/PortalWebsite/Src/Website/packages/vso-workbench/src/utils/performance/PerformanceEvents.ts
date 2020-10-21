export enum PerformanceEventNames {
    InitGetPlatformInfo = 'get platform info',
    InitUpdateFavicon = 'update favicon',
    OpenSshChannel = 'open SSH channel',
    FinishingConnection = 'finishing up connection process',
    VSCodeServerStartup = 'start vscode server',
};

export enum PerformanceEventIds {
    // global performance event - fired when the portal SPA started
    Start = 'Start',
    // inital platform info retrieval event
    InitGetPlatformInfo = 'InitGetPlatformInfo',
    // time between `start` and when `terminal`/`extensions` available
    InitTimeToRemoteExtensions = 'InitTimeToRemoteExtensions',
    // time for the `workbench component` to initialize,
    // including `connection` time,
    // excluding time to `terminal`/`extensions`
    // excluding `splash screen` time
    WorkbenchComponent = 'WorkbenchComponent',
    // time for the `workbench page` to initialize,
    // including `connection` time,
    // including `splash screen` time
    WorkbenchPage = 'WorkbenchPage',
    // time for workbench components initialization
    WorkbenchPageInitialization = 'WorkbenchPageInitialization',
    // time for openning another SSH channel, happens after the connection established
    OpenSshChannel = 'OpenSshChannel',
    // time to finish up the connection, happens after the connection established
    FinishingConnection = 'FinishingConnection',
    // time to start vscode server on the remote side
    // including the RPC message travel time
    // including downloading vscode server, if needed
    // including installing extensions, if needed
    VSCodeServerStartup = 'VSCodeServerStartup',
    // time for vscode client to vscode server protocol upgrade, happens after the connection established
    VSCodeClientServerHandshake = 'VSCodeClientServerHandshake',
    // time to start a codespace (HTTP call to the service)
    StartCodespace = 'StartCodespace',
    // time to initialize vscode worbench on client side
    // including conenction times
    // excluding time to `terminal`/`extensions`
    VSCodeInitialization = 'VSCodeInitialization',
    // time to get codespace info (HTTP call to the service)
    GetEnvironmentInfo1 = 'GetEnvironmentInfo1',
    // time to get codespace info (HTTP call to the service)
    GetEnvironmentInfo2 = 'GetEnvironmentInfo2',
    // time to get LiveShare workspace info for conneciton (HTTP call to the service)
    GetLiveshareWorkspaceInfo = 'GetLiveshareWorkspaceInfo',
    // time to join the LiveShare session
    // excluding time to establish connection to the relay
    WorkbenchClientConnection = 'WorkbenchClientConnection',
};
