//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

import * as rpc from 'vscode-jsonrpc';
import { Buffer } from 'buffer';
import { DataWriter } from './DataWriter';
import { IDataChannel } from './IDataChannel';

const contentLengthHeaderPrefix = 'Content-Length: ';
const headersSeparator = '\r\n\r\n';


class RpcMessageReader implements rpc.MessageReader {
    private readonly errorEmitter = new rpc.Emitter<Error>();
    private readonly closeEmitter = new rpc.Emitter<void>();
    private readonly partialMessageEmitter = new rpc.Emitter<any>();
    private readonly eventRegistration: rpc.Disposable;
    private callback: rpc.DataCallback | null = null;
    private readonly messageBuffer = new DataWriter(Buffer.alloc(1024));
    private headersLength: number | null = null;
    private messageLength: number | null = null;

    constructor(public channel: IDataChannel) {
        this.onError = this.errorEmitter.event;
        this.onClose = this.closeEmitter.event;
        this.onPartialMessage = this.partialMessageEmitter.event;
        this.eventRegistration = this.channel.onDataReceived(this.onDataReceived.bind(this));

        this.channel.onClosed((e) => {
            if (e.error) {
                this.errorEmitter.fire(e.error);
                return;
            }

            this.closeEmitter.fire();
        });
    }

    public readonly onError: rpc.Event<Error>;
    public readonly onClose: rpc.Event<void>;
    public readonly onPartialMessage: rpc.Event<any>;

    public listen(callback: rpc.DataCallback): void {
        this.callback = callback;
    }

    public dispose(): void {
        if (this.eventRegistration) {
            this.eventRegistration.dispose();
        }
    }

    private onDataReceived(data: Buffer) {
        const startingPosition = this.messageBuffer.position;
        this.messageBuffer.write(data);

        if (startingPosition > 0) {
            data = this.messageBuffer.toBuffer();
        }

        if (this.messageLength === null) {
            const headersEnd = data.indexOf(headersSeparator);
            if (headersEnd < 0) {
                return; // Wait for more data.
            }

            const headers = data.slice(0, headersEnd).toString();
            if (!headers.startsWith(contentLengthHeaderPrefix)) {
                throw new Error(`Message does not start with JSON-RPC headers.\n${headers}`);
            }

            this.headersLength = headersEnd + headersSeparator.length;
            this.messageLength = parseInt(
                headers.substr(
                    contentLengthHeaderPrefix.length,
                    headersEnd - contentLengthHeaderPrefix.length,
                ),
                10,
            );
        }

        const position = this.messageBuffer.position;
        const totalLength = this.headersLength! + this.messageLength;

        if (position >= totalLength) {
            if (this.callback) {
                const messageJson = data.slice(this.headersLength!, totalLength).toString();
                let message: rpc.Message;
                try {
                    message = JSON.parse(messageJson);
                } catch (e) {
                    throw new Error(`Failed to parse JSON-RPC message: ${e.message}\n${messageJson}`);
                }
                this.callback(message);
            }

            this.messageLength = null;
            this.messageBuffer.position = 0;

            if (position > totalLength) {
                // Recursively receive the remaining data, which will cause it
                // to be copied to the beginning of the buffer;
                this.onDataReceived(data.slice(totalLength));
            }
        }
    }
}

class RpcMessageWriter implements rpc.MessageWriter {
    private readonly errorEmitter = new rpc.Emitter<
        [Error, rpc.Message | undefined, number | undefined]
    >();
    private readonly closeEmitter = new rpc.Emitter<void>();

    constructor(public channel: IDataChannel) {
        this.onError = this.errorEmitter.event;
        this.onClose = this.closeEmitter.event;

        this.channel.onClosed((e) => {
            if (e.error) {
                this.errorEmitter.fire([
                    e.error,
                    (e.errorMessage && { jsonrpc: e.errorMessage }) || undefined,
                    e.exitStatus,
                ]);
                return;
            }

            this.closeEmitter.fire();
        });
    }

    public onError: rpc.Event<[Error, rpc.Message | undefined, number | undefined]>;

    public onClose: rpc.Event<void>;

    public write(message: rpc.Message): void {
        const messageJson = JSON.stringify(message);
        const messageData = Buffer.from(messageJson);
        const headerData = Buffer.from(
            contentLengthHeaderPrefix + messageData.length + headersSeparator,
        );
        const data = Buffer.alloc(headerData.length + messageData.length);
        headerData.copy(data, 0);
        messageData.copy(data, headerData.length);
        this.channel.send(data).catch((e: Error) => {
            this.errorEmitter.fire([
                e,
                undefined,
                undefined,
            ]);
        });
    }

    public dispose(): void {}
}

export class RpcMessageStream {
    constructor(channel: IDataChannel) {
        this.reader = new RpcMessageReader(channel);
        this.writer = new RpcMessageWriter(channel);
    }

    public readonly reader: rpc.MessageReader;
    public readonly writer: rpc.MessageWriter;
}
