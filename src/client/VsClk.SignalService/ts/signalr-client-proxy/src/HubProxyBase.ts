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

function toPascal(name: string): string {
    return name.substr(0, 1).toUpperCase() + name.substr(1);
}

export function keysToCamel(o: any): any {
    return convertKeys(o, toCamel);
}

export function keysToPascal(o: any): any {
    return convertKeys(o, toPascal);
}

function convertKeys(o: any, keyConverter: (key: string) => string): any {
    if (isArray(o)) {
        return o.map((i) => {
            return convertKeys(i, keyConverter);
        });       
    } else if (isObject(o)) {
        const n: any = {};
        Object.keys(o)
        .forEach((k) => {
            let value = o[k];
            if (isObject(value)) {
                value = convertKeys(value, keyConverter);
            }
            n[keyConverter(k)] = value;
        });

        return n;
    }

    return o;
}