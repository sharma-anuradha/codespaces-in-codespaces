import { IDisposable } from './IDisposable';

export interface IHubProxyConnection {
    readonly isConnected: boolean;
    connect(): Promise<void>;
    onConnectionStateChanged(callback: () => Promise<void>): IDisposable;
}