import { MessageConnection, CancellationToken, Event } from 'vscode-jsonrpc';
export interface RequestHandler {
    (args: any[], cancellation: CancellationToken): any | Promise<any>;
}
export interface NotifyHandler {
    (args: object): void;
}
/**
 * A service that is provided by the host for use by guests.
 */
export interface SharedService {
    /**
     * A shared service is available when a sharing session is active as a Host.
     */
    readonly isServiceAvailable: boolean;
    readonly onDidChangeIsServiceAvailable: Event<boolean>;
    /**
     * Registers a callback to be invoked when a request is sent to the service.
     *
     * @param name Request method name
     */
    onRequest(name: string, handler: RequestHandler): void;
    /**
     * Registers a callback to be invoked when a notification is sent to the service.
     *
     * @param name Notify event name
     */
    onNotify(name: string, handler: NotifyHandler): void;
    /**
     * Sends a notification (event) from the service. Does not wait for a response.
     *
     * If no sharing session is active, this method does nothing.
     *
     * @param name notify event name
     * @param args notify event args object
     */
    notify(name: string, args: object): void;
}
/**
 * A proxy that allows guests to access a host-provided service.
 */
export interface SharedServiceProxy {
    /**
     * A shared service proxy is available when a sharing session is active as a
     * Guest, and the Host has shared a service with the same name.
     */
    readonly isServiceAvailable: boolean;
    readonly onDidChangeIsServiceAvailable: Event<boolean>;
    /**
     * Registers a callback to be invoked when a notification is sent by the service.
     *
     * @param name notify event name
     */
    onNotify(name: string, handler: NotifyHandler): void;
    /**
     * Sends a request (method call) to the service and waits for a response.
     *
     * @param name request method name
     *
     * @returns a promise that waits asynchronously for a response
     *
     * @throws SharedServiceProxyError if the service is not currently available
     * (because there is no active sharing session or no peer has provided the service)
     *
     * @throws SharedServiceResponseError (via rejected promise) if the service's
     * request handler throws an error
     */
    request(name: string, args: any[], cancellation?: CancellationToken): Promise<any>;
    /**
     * Sends a notification (event) to the service. (Does not wait for a response.)
     *
     * If the service is not currently available (either because there is
     * no active sharing session or because no peer has provided the service)
     * then this method does nothing.
     *
     * @param name notify event name
     * @param args notify event args object
     */
    notify(name: string, args: object): void;
}
/**
 * Error thrown by a proxy when a request to a shared service cannot be made
 * because the service is not available or cannot be reached.
 */
export interface SharedServiceProxyError extends Error {}
/**
 * Error thrown by a proxy when a shared service's request handler threw an error.
 * The remote message and remote stack are propagated back to the proxy.
 */
export interface SharedServiceResponseError extends Error {
    remoteStack?: string;
}
/**
 * RPC variables are intentionally NOT private members of public API objects,
 * to prevent extensions from trivially using the private members to make
 * arbitrary RPC calls.
 */
const rpc = {
    connection: <MessageConnection | null>null,
};
