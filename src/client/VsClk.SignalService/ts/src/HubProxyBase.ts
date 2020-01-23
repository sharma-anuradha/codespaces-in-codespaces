import * as signalR from '@microsoft/signalr';

export class HubProxyBase {
    private static readonly invokeHubMethodAsync = 'InvokeHubMethodAsync';

    constructor(
        public readonly hubConnection: signalR.HubConnection,
        protected readonly logger?: signalR.ILogger,
        protected readonly hubName?: string) {
    }

    protected toHubMethodName(methodName: string): string {
        return this.hubName ? `${this.hubName}.${methodName}` : methodName;
    }

    public invoke<T = any>(methodName: string, ...args: any[]): Promise<T> {
        if (this.hubName) {
            return this.hubConnection.invoke<T>(HubProxyBase.invokeHubMethodAsync, this.toHubMethodName(methodName), args );
        } else {
            return this.hubConnection.invoke<T>(methodName, ...args);
        }
    }

    public send(methodName: string, ...args: any[]): Promise<void> {
        if (this.hubName) {
            return this.hubConnection.send(HubProxyBase.invokeHubMethodAsync, this.toHubMethodName(methodName), args );
        } else {
            return this.hubConnection.send(methodName, ...args);
        }     
    }
}