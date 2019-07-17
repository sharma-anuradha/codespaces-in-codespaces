import * as signalR from '@aspnet/signalr';
import { connect } from './HubConnectionHelpers';

export class HubClient {
    private isRunningFlag = false;
    private attemtConnectionCallbacks: Array<(retries: number, backoffTime?: number, error?: Error) => Promise<number>>;
    private connectionStateCallbacks: Array<() => Promise<void>>;

    constructor(
        public readonly hubConnection: signalR.HubConnection,
        private readonly logger?: signalR.ILogger) {
        this.attemtConnectionCallbacks = [];
        this.connectionStateCallbacks = [];

        hubConnection.onclose((error?: Error) => this.onClosed(error));
    }

    public static createWithUrl(url: string, logger?: signalR.ILogger): HubClient  {
        return new HubClient(new signalR.HubConnectionBuilder().withUrl(url).build(), logger);
    }

    public static createWithUrlAndToken(url: string, accessTokenFactory: () => string | Promise<string>, logger?: signalR.ILogger) {
        return new HubClient(new signalR.HubConnectionBuilder()
        .withUrl(url, <signalR.IHttpConnectionOptions> {
            accessTokenFactory
        }).build(), logger);
    }

    public start(): Promise<void> {
        if (this.logger) {
            this.logger.log(signalR.LogLevel.Debug, `HubClient.start`);           
        }

        this.isRunningFlag = true;
        return this.attemptConnect();
    }

    public async stop(): Promise<void> {
        if (this.logger) {
            this.logger.log(signalR.LogLevel.Debug, `HubClient.stop`);           
        }
        
        if (this.isRunningFlag) {
            this.isRunningFlag = false;
            await this.hubConnection.stop();
        }
    }

    public get state(): signalR.HubConnectionState {
        return this.hubConnection.state;
    }

    public get isConnected() {
        return this.state === signalR.HubConnectionState.Connected;
    }

    public get isRunning(): boolean { 
        return this.isRunningFlag;
    }

    public onAttemtConnection(callback: (retries: number, backoffTime?: number, error?: Error) => Promise<number>) {
        if (callback) {
            this.attemtConnectionCallbacks.push(callback);
        }
    }

    public onConnectionStateChanged(callback: () => Promise<void>) {
        if (callback) {
            this.connectionStateCallbacks.push(callback);
        }
    }

    private async attemptConnect(): Promise<void> {
        await connect(
            this.hubConnection,
            async (retries: number, backoffTime?: number, error?: Error) => {
                if (!this.isRunningFlag) {
                    return -1;
                }
                
                let result = 0;
                if (backoffTime !== undefined) {
                    result = backoffTime; 
                }
                
                for (const callback of this.attemtConnectionCallbacks) {
                    result = await callback(retries, backoffTime, error);
                }

                return result;
            },
            -1,
            5000,
            60000,
            this.logger ? this.logger : signalR.NullLogger.instance);
        
        if (this.isConnected) {
            for (const callback of this.connectionStateCallbacks) {
                await callback();
            }
        }
    }

    private async onClosed(error?: Error) {
        if (this.logger) {
            this.logger.log(signalR.LogLevel.Debug, `onClosed error:${error}`);           
        }
        
        for (const callback of this.connectionStateCallbacks) {
            await callback();
        }

        this.attemptConnect();
    }
}

