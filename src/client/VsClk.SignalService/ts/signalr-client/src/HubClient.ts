import * as signalR from '@microsoft/signalr';
import { connect } from './HubConnectionHelpers';
import { IHubProxy } from './IHubProxy';
import { IHubProxyConnection } from './IHubProxyConnection';
import { HubProxy } from './HubProxy';

export class HubClient implements IHubProxyConnection {
    private isRunningFlag = false;
    private attemptConnectionCallbacks: Array<(retries: number, backoffTime?: number, error?: Error) => Promise<void>>;
    private connectionStateCallbacks: Array<() => Promise<void>>;
    private hubProxyInstance: IHubProxy;

    constructor(
        public readonly hubConnection: signalR.HubConnection,
        private readonly logger?: signalR.ILogger) {
        this.attemptConnectionCallbacks = [];
        this.connectionStateCallbacks = [];

        hubConnection.onclose((error?: Error) => this.onClosed(error));
        this.hubProxyInstance = new HubProxy(this);
    }

    public static createWithHub(hubConnection: signalR.HubConnection, logger?: signalR.ILogger) {
        return new HubClient(hubConnection, logger);
    }

    public static create(url: string, httpConnectionOptions: signalR.IHttpConnectionOptions, logger?: signalR.ILogger) {
        if (logger) {
            logger.log(signalR.LogLevel.Debug, `createWithOptions url:${url}`);           
        }

        return new HubClient(new signalR.HubConnectionBuilder().withUrl(url, httpConnectionOptions).build(), logger);
    }

    public get hubProxy(): IHubProxy {
        return this.hubProxyInstance;
    }

    public start(): Promise<void> {
        if (!this.isRunning) {
            this.log(signalR.LogLevel.Debug, `HubClient.start`);           
            this.isRunningFlag = true;
            if (this.state !== signalR.HubConnectionState.Connected) {
                return this.attemptConnect();
            }
        }

        return Promise.resolve();
    }

    public async stop(): Promise<void> {
        if (this.isRunning) {
            this.log(signalR.LogLevel.Debug, `HubClient.stop`);
            this.isRunningFlag = false;
           
            await this.hubConnection.stop();
        }        
    }

    public get state(): signalR.HubConnectionState {
        return this.hubConnection.state;
    }

    public get isConnected(): boolean {
        return this.state === signalR.HubConnectionState.Connected;
    }

    public get isRunning(): boolean { 
        return this.isRunningFlag;
    }

    public onAttemtConnection(callback: (retries: number, backoffTime?: number, error?: Error) => Promise<void>) {
        if (callback) {
            this.attemptConnectionCallbacks.push(callback);
        }
    }

    public onConnectionStateChanged(callback: () => Promise<void>) {
        if (callback) {
            this.connectionStateCallbacks.push(callback);
        }
    }

    private async attemptConnect(): Promise<void> {
        this.log(signalR.LogLevel.Debug, `attemptConnect`);
        await this.fireAttemptConnection(0, 0, undefined);
        await connect(
            this.hubConnection,
            async (retries: number, backoffTime?: number, error?: Error) => {
                if (!this.isRunningFlag) {
                    return -1;
                }             
                await this.fireAttemptConnection(retries, backoffTime, error);
                return backoffTime !== undefined ? backoffTime : 0;
            },
            -1,
            5000,
            60000,
            this.logger ? this.logger : signalR.NullLogger.instance);
        
        if (this.isConnected) {
            await this.fireConnectionStateChanged();
        }
    }

    private async onClosed(error?: Error) {
        this.log(signalR.LogLevel.Debug, `onClosed error:${error}`);                
        await this.fireConnectionStateChanged();
        if (this.isRunning) {
            this.attemptConnect();
        }
    }

    private log(logLevel: signalR.LogLevel, message: string): void {
        if (this.logger) {
            this.logger.log(logLevel, message);           
        }
    }

    private async fireConnectionStateChanged(): Promise<void> {
        for (const callback of this.connectionStateCallbacks) {
            await callback();
        }   
    }

    private async fireAttemptConnection(retries: number, backoffTime?: number, error?: Error): Promise<void> {
        for (const callback of this.attemptConnectionCallbacks) {
            await callback(retries, backoffTime, error);
        }     
    }
}

