import { SshChannel, SshDisconnectReason } from '@vs/vs-ssh';

import { Event, Emitter } from 'vscode-jsonrpc';
import { EnvConnector } from '../ts-agent';

import { trace } from '../utils/trace';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import { bufferToArrayBuffer } from '../utils/bufferToArrayBuffer';

const envConnector = new EnvConnector();

export interface IWebSocketFactory {
    create(url: string): IWebSocket;
}

export interface IWebSocket {
    readonly onData: Event<ArrayBuffer>;
    readonly onOpen: Event<void>;
    readonly onClose: Event<void>;
    readonly onError: Event<any>;

    send(data: ArrayBuffer | ArrayBufferView): void;
    close(): void;
}

export class VSLSWebSocket implements IWebSocket {
    private readonly _onData = new Emitter<ArrayBuffer>();
    public readonly onData: Event<ArrayBuffer> = this._onData.event;

    private readonly _onOpen = new Emitter<void>();
    public readonly onOpen: Event<void> = this._onOpen.event;

    private readonly _onClose = new Emitter<void>();
    public readonly onClose: Event<void> = this._onClose.event;

    private readonly _onError = new Emitter<void>();
    public readonly onError: Event<any> = this._onError.event;

    private static socketCnt: number = 0;

    private channel!: SshChannel;

    public send(data: ArrayBuffer | ArrayBufferView) {
        const bufferData = Buffer.from(data as ArrayBuffer);
        trace(
            `Ssh channel [${VSLSWebSocket.socketCnt}][${
                this.isChannelDisposed
            }] send: \n${bufferData.toString()}\n`
        );
        this.channel.send(bufferData);
    }

    public close() {
        trace(`Ssh channel[${VSLSWebSocket.socketCnt}] closed by the VSCode shell.`);
        this.channel.session.close(
            SshDisconnectReason.byApplication,
            'Workbench requested to close the connection.'
        );
    }

    private isChannelDisposed: boolean = false;

    constructor(
        url: string,
        private accessToken: string,
        private environmentInfo: ICloudEnvironment
    ) {
        this.initializeChannel(url);
        VSLSWebSocket.socketCnt++;
    }

    private async initializeChannel(url: string) {
        url = url.replace(/skipWebSocketFrames=false/gim, 'skipWebSocketFrames=true');

        const delimiter = '\r\n';

        const buffer = Buffer.alloc(16);
        for (let i = 0; i < 16; i++) {
            // This matches vscode expectations and isn't meant for security purposes.
            // tslint:disable-next-line: insecure-random
            buffer[i] = Math.round(Math.random() * 256);
        }
        const nonce = buffer.toString('base64');

        const requestArray = [
            `GET ${url} HTTP/1.1`,
            `Connection: Upgrade`,
            `Upgrade: websocket`,
            `Sec-WebSocket-Key: ${nonce}`,
            delimiter,
        ];

        const requestString = requestArray.join(delimiter);

        await envConnector.ensureConnection(this.environmentInfo, this.accessToken);
        const channel: SshChannel = await envConnector.sendHandshakeRequest(requestString);

        channel.onDataReceived((data: Buffer) => {
            trace(`SSh channel [${VSLSWebSocket.socketCnt}] received: \n${data.toString()}\n`);
            channel.adjustWindow(data.length);
            this._onData.fire(bufferToArrayBuffer(data));
        });

        this._onOpen.fire();
        trace(`**[${VSLSWebSocket.socketCnt}] Ssh channel open.`);

        channel.onClosed(async () => {
            this.isChannelDisposed = true;
            trace(`[${VSLSWebSocket.socketCnt}] Ssh channel closed.`);
            this._onClose.fire();
        });

        this.channel = channel;
    }
}
