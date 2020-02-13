import { IHubProxyConnection } from './IHubProxyConnection';
export interface IHubProxy extends IHubProxyConnection {
    send(methodName: string, ...args: any[]): Promise<void>;
    invoke<T = any>(methodName: string, ...args: any[]): Promise<T>;
    on(methodName: string, newMethod: (...args: any[]) => void): void;
}