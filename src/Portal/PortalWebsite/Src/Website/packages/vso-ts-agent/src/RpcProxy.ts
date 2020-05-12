import * as rpc from 'vscode-jsonrpc';

import { vsls } from 'vso-client-core';

/**
 * Defines error codes returned by the VSLS agent implemention of JSON-RPC,
 * which are a super-set of standard JSON-RPC error codes.
 */
export enum RpcErrorCodes {
    ParseError = rpc.ErrorCodes.ParseError,
    InvalidRequest = rpc.ErrorCodes.InvalidRequest,
    MethodNotFound = rpc.ErrorCodes.MethodNotFound,
    InvalidParams = rpc.ErrorCodes.InvalidParams,
    InternalError = rpc.ErrorCodes.InternalError,
    ServerNotInitialized = rpc.ErrorCodes.ServerNotInitialized,
    UnknownErrorCode = rpc.ErrorCodes.UnknownErrorCode,
    RequestCancelled = rpc.ErrorCodes.RequestCancelled,
    MessageWriteError = rpc.ErrorCodes.MessageWriteError,
    MessageReadError = rpc.ErrorCodes.MessageReadError,

    // VSLS extended RPC error codes
    ServiceNotAvailable = -32099,
    InvocationException = -32098,
}

/**
 * Base class for RPC service proxies.
 */
export class RpcProxy {
    private constructor(
        public readonly connection: rpc.MessageConnection,
        public readonly serviceName: string,
    ) {}

    /**
     * Creates a proxy for an RPC service.
     *
     * @param serviceInfo Information about the service contract
     * @param client RPC client
     */
    public static create<T>(
        serviceInfo: vsls.ServiceInfo<T>,
        connection: rpc.MessageConnection,
    ): T {
        if (!(serviceInfo && serviceInfo.name)) {
            throw new Error('Missing RPC service name.');
        }

        const proxy = new RpcProxy(connection, serviceInfo.name);

        // Generate async methods for requests.
        for (let methodName of serviceInfo.methods) {
            const methodPropertyName = `${methodName}Async`;
            // tslint:disable-next-line: no-function-expression
            (<any>proxy)[methodPropertyName] = function() {
                // Detect whether optional cancellation token was supplied, and if so strip from args.
                let args: any[];
                let cancellationToken: rpc.CancellationToken | null = arguments[arguments.length - 1];
                if (
                    cancellationToken &&
                    typeof cancellationToken === 'object' &&
                    typeof cancellationToken.isCancellationRequested === 'boolean'
                ) {
                    args = Array.prototype.slice.call(arguments, 0, arguments.length - 1);
                } else {
                    args = Array.prototype.slice.call(arguments, 0, arguments.length);
                    cancellationToken = null;
                }

                // Detect whether optional progress was supplied, and if so strip from args.
                let progress: { report: (value: any) => void } | null = args[args.length - 1];
                if (
                    progress &&
                    typeof progress === 'object' &&
                    typeof progress.report === 'function'
                ) {
                    args.splice(args.length - 1, 1);
                } else {
                    progress = null;
                }

                const serviceAndMethodName = proxy.serviceName + '.' + methodName;

                // Note progress is not implemented, because this code bypasses the RpcClient class
                // that VSLS uses.

                // The vscode-jsonrpc sendRequest() method can only detect a cancellation token argument
                // if it is not null.
                if (cancellationToken) {
                    return connection.sendRequest<T>(serviceAndMethodName, args, cancellationToken);
                } else {
                    return connection.sendRequest<T>(serviceAndMethodName, args);
                }
            };
        }

        // Generate methods for method-style notifications.
        for (let methodName of serviceInfo.voidMethods || []) {
            // tslint:disable-next-line: no-function-expression
            (<any>proxy)[methodName] = function() {
                let args: any[] = Array.prototype.slice.call(arguments, 0, arguments.length);
                const serviceAndMethodName = proxy.serviceName + '.' + methodName;
                proxy.connection.sendNotification(serviceAndMethodName, args);
            };
        }

        // Generate events for event-style notifications.
        for (let eventName of serviceInfo.events) {
            const emitter = new rpc.Emitter<any>();
            const eventPropertyName = `on${eventName.substr(0, 1).toUpperCase()}${eventName.substr(
                1,
            )}`;
            (<any>proxy)[eventPropertyName] = emitter.event;

            const serviceAndEventName = proxy.serviceName + '.' + eventName;

            proxy.connection.onNotification(serviceAndEventName, (...args: any[]) => {
                const eventArgs = args[0];
                emitter.fire(eventArgs);
            });
        }

        return <T>(<any>proxy);
    }

    /**
     * Sends a notification (event) from this client to the service.
     *
     * (This is a static method because RPC contract interfaces do not define methods
     * for reverse notifications.)
     */
    public static notify<T>(proxy: T, eventName: string, args: any): void {
        const rpcProxy = (proxy as any) as RpcProxy;
        const serviceAndMethodName = rpcProxy.serviceName + '.' + eventName;
        return rpcProxy.connection.sendNotification(serviceAndMethodName, args);
    }
}
