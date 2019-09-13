//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
'use strict';

import {
    ErrorCodes,
    ResponseError,
    MessageConnection,
    CancellationToken,
    Event,
    Emitter,
} from 'vscode-jsonrpc';
import {
    SharedService,
    RequestHandler,
    NotifyHandler,
    SharedServiceProxy,
} from '../contracts/vsls-types';

/**
 * RPC variables are intentionally NOT private members of public API objects,
 * to prevent extensions from trivially using the private members to make
 * arbitrary RPC calls.
 */
const rpc = {
    connection: <MessageConnection | null>null,
};

/**
 * Implements both the service and service proxy interfaces.
 */
export class SharedServiceImp implements SharedService, SharedServiceProxy {
    private isAvailable: boolean = false;
    private isAvailableChange = new Emitter<boolean>();

    public constructor(public readonly name: string, connection: MessageConnection) {
        rpc.connection = connection;

        // Ensure the name property cannot be modified.
        Object.defineProperty(this, 'name', {
            enumerable: false,
            configurable: false,
            writable: false,
        });
    }

    public get isServiceAvailable(): boolean {
        return this.isAvailable;
    }
    public get onDidChangeIsServiceAvailable(): Event<boolean> {
        return this.isAvailableChange.event;
    }

    public set isServiceAvailable(value: boolean) {
        this.isAvailable = value;
    }

    public fireIsAvailableChange() {
        this.isAvailableChange.fire(this.isAvailable);
    }

    public onRequest(name: string, handler: RequestHandler): void {
        const rpcName = this.makeRpcName(name);

        rpc.connection!.onRequest(rpcName, (...args: any[]) => {
            // Separate the cancellation token from the end of the args array.
            const [cancellation] = args.splice(args.length - 1, 1);

            try {
                return handler(args, cancellation);
            } catch (e) {
                let stack = e.stack;
                if (stack) {
                    // Strip off the part of the stack that is not in the extension code.
                    stack = stack.replace(
                        new RegExp('\\s+at ' + SharedServiceImp.name + '(.*\n?)+'),
                        ''
                    );
                }

                return new ResponseError(ErrorCodes.UnknownErrorCode, e.message, stack);
            }
        });
    }

    public onNotify(name: string, handler: NotifyHandler): void {
        const rpcName = this.makeRpcName(name);

        rpc.connection!.onNotification(rpcName, (...argsArray: any[]) => {
            const args: any = argsArray[0];

            try {
                handler(args);
            } catch (e) {
                // Notifications have no response, so no error details are returned.
            }
        });
    }

    public async request(
        name: string,
        args: any[],
        cancellation?: CancellationToken
    ): Promise<any> {
        const rpcName = this.makeRpcName(name);

        if (!this.isServiceAvailable) {
            throw new SharedServiceProxyError("Service '" + this.name + "' is not available.");
        }

        let responsePromise: Thenable<any>;
        try {
            // The vscode-jsonrpc sendRequest() method can only detect a cancellation token argument
            // if it is not null.
            if (cancellation) {
                responsePromise = rpc.connection!.sendRequest(rpcName, args, cancellation);
            } else {
                responsePromise = rpc.connection!.sendRequest(rpcName, args);
            }
        } catch (e) {
            throw new SharedServiceProxyError(e.message);
        }

        let response: any;
        try {
            response = await responsePromise;
        } catch (e) {
            throw new SharedServiceResponseError(e.message, e.data);
        }

        return response;
    }

    public notify(name: string, args: any): void {
        const rpcName = this.makeRpcName(name);

        if (!this.isServiceAvailable) {
            // Notifications do nothing when the service is not available.
            return;
        }

        try {
            rpc.connection!.sendNotification(rpcName, [args]);
        } catch (e) {
            throw new SharedServiceProxyError(e.message);
        }
    }

    private makeRpcName(name: string): string {
        return this.name + '.' + name;
    }
}

export class SharedServiceProxyError extends Error {
    constructor(message: string) {
        super(message);
        this.name = SharedServiceProxyError.name;
    }
}

export class SharedServiceResponseError extends Error {
    constructor(message: string, public remoteStack?: string) {
        super(message);
        this.name = SharedServiceResponseError.name;
    }
}
