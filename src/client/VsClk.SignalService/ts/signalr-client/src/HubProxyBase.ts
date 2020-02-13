import { IHubProxy } from './IHubProxy';
import { IServiceProxyBase } from './IServiceProxyBase';

export class HubProxyBase implements IServiceProxyBase {
    private static readonly invokeHubMethodAsync = 'InvokeHubMethodAsync';

    constructor(
        public readonly hubProxy: IHubProxy,
        protected readonly logger?: signalR.ILogger,
        protected readonly hubName?: string) {
    }

    protected toHubMethodName(methodName: string): string {
        return this.hubName ? `${this.hubName}.${methodName}` : methodName;
    }

    public invoke<T = any>(methodName: string, ...args: any[]): Promise<T> {
        if (this.hubName) {
            return this.hubProxy.invoke<T>(HubProxyBase.invokeHubMethodAsync, this.toHubMethodName(methodName), args );
        } else {
            return this.hubProxy.invoke<T>(methodName, ...args);
        }
    }

    public send(methodName: string, ...args: any[]): Promise<void> {
        if (this.hubName) {
            return this.hubProxy.send(HubProxyBase.invokeHubMethodAsync, this.toHubMethodName(methodName), args );
        } else {
            return this.hubProxy.send(methodName, ...args);
        }     
    }
}