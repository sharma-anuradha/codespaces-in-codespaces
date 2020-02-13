
import { IHubProxy } from './IHubProxy';
import { HubClient } from './HubClient';

import * as signalR from '@microsoft/signalr';

export class HubProxy implements IHubProxy {

    constructor(private readonly hubClient: HubClient) {   
    }

    private get hubConnection() {
        return this.hubClient.hubConnection;
    }

    public get isConnected(): boolean {
        return this.hubClient.isConnected;
    }

    public onConnectionStateChanged(callback: () => Promise<void>): void {
        this.hubClient.onConnectionStateChanged(callback);
    }

    public send(methodName: string, ...args: any[]): Promise<void> {
        return this.hubConnection.send(methodName, ...args);
    }

    public invoke<T = any>(methodName: string, ...args: any[]): Promise<T> {
        return this.hubConnection.invoke<T>(methodName, ...args);
    }

    public on(methodName: string, newMethod: (...args: any[]) => void): void {
        this.hubConnection.on(methodName, newMethod);
    }
}