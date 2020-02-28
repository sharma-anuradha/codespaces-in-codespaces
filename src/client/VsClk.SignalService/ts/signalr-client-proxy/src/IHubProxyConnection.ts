import { IDisposable } from './IDisposable';

export interface IHubProxyConnection {
    readonly isConnected: boolean;
    onConnectionStateChanged(callback: () => Promise<void>): IDisposable;
}