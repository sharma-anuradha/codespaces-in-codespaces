import { IHubProxy } from './IHubProxy';
import { IServiceProxyBase } from './IServiceProxyBase';
import { ILogger } from './ILogger';
import { isObject, isArray } from 'util';

export class HubProxyBase implements IServiceProxyBase {
    private static readonly invokeHubMethodAsync = 'InvokeHubMethodAsync';

    constructor(
        public readonly hubProxy: IHubProxy,
        public readonly logger?: ILogger,
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

    public async invokeKeysToCamel<T = any>(methodName: string, ...args: any[]): Promise<T> {
        const result = await this.invoke<any>(methodName, ...args);
        return <T>keysToCamel(result);
    }
}

function toCamel(name: string): string {
    return name.substr(0, 1).toLowerCase() + name.substr(1);
}

export function keysToCamel(o: any): any {
    if (isArray(o)) {
        return o.map((i) => {
            return keysToCamel(i);
        });       
    } else if (isObject(o)) {
        const n: any = {};
        Object.keys(o)
        .forEach((k) => {
            let value = o[k];
            if (isObject(value)) {
                value = keysToCamel(value);
            }
            n[toCamel(k)] = value;
        });

        return n;
    }
}