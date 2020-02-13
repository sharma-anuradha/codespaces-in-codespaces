export interface IHubProxyConnection {
    readonly isConnected: boolean;
    onConnectionStateChanged(callback: () => Promise<void>): void;
}