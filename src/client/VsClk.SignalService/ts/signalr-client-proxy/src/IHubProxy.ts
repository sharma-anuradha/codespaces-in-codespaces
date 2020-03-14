import { IHubProxyConnection } from './IHubProxyConnection';
import { IDisposable } from './IDisposable';

export enum HubMethodOption {
    Send = 0,
    Invoke = 1,
}

export interface IHubProxy extends IHubProxyConnection {
    send(methodName: string, ...args: any[]): Promise<void>;
    invoke<T = any>(methodName: string, ...args: any[]): Promise<T>;
    on(methodName: string, newMethod: (...args: any[]) => void): IDisposable;
}