
import { IHubProxy, IDisposable } from '@vs/vso-signalr-client-proxy';
import { HubClient } from './HubClient';

export class HubProxy implements IHubProxy {

    constructor(private readonly hubClient: HubClient) {   
    }

    private get hubConnection() {
        return this.hubClient.hubConnection;
    }

    public get isConnected(): boolean {
        return this.hubClient.isConnected;
    }
    public connect(): Promise<void> {
        return this.hubClient.connect();
    }

    public onConnectionStateChanged(callback: () => Promise<void>): IDisposable {
        return this.hubClient.onConnectionStateChanged(callback);
    }

    public send(methodName: string, ...args: any[]): Promise<void> {
        return this.hubConnection.send(methodName, ...args);
    }

    public invoke<T = any>(methodName: string, ...args: any[]): Promise<T> {
        return this.hubConnection.invoke<T>(methodName, ...args);
    }

    public on(methodName: string, newMethod: (...args: any[]) => void): IDisposable {
        this.hubConnection.on(methodName, newMethod);
        return {
            dispose: () => this.hubConnection.off(methodName)
        };
    }
}