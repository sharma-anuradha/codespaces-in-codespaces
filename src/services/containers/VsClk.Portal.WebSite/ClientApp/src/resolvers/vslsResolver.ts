import { SshChannel } from '@vs/vs-ssh';
import { Event, Emitter } from 'vscode-jsonrpc';

import { EnvConnector } from '../ts-agent/envConnector';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import { bufferToArrayBuffer } from '../utils/bufferToArrayBuffer';
import { trace as baseTrace } from '../utils/trace';
import { createTrace } from '../utils/createTrace';

const TRACE_NAME = 'vsls-web-socket';
const { verbose, info, error } = createTrace(TRACE_NAME);

const logContent = baseTrace.extend(`${TRACE_NAME}:trace:content`);
logContent.log =
    // tslint:disable-next-line: no-console
    typeof console.debug === 'function' ? console.debug.bind(console) : console.log.bind(console);

export const envConnector = new EnvConnector();

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
    private id: number;

    private readonly _onData = new Emitter<ArrayBuffer>();
    public readonly onData: Event<ArrayBuffer> = this._onData.event;

    private readonly _onOpen = new Emitter<void>();
    public readonly onOpen: Event<void> = this._onOpen.event;

    private readonly _onClose = new Emitter<void>();
    public readonly onClose: Event<void> = this._onClose.event;

    private readonly _onError = new Emitter<Error>();
    public readonly onError: Event<any> = this._onError.event;

    private static socketCnt: number = 0;

    private channel!: SshChannel;

    private getWebSocketIdentifier() {
        return `${this.id}:${this.url}`;
    }

    public send(data: ArrayBuffer | ArrayBufferView) {
        const bufferData = Buffer.from(data as ArrayBuffer);

        verbose(`[${this.getWebSocketIdentifier()}] Ssh channel sending data.`);
        logContent(`[${this.getWebSocketIdentifier()}]\n\n${bufferData.toString()}`);

        this.channel.send(bufferData);
    }

    public close() {
        info(`[${this.getWebSocketIdentifier()}] Ssh channel closed by VSCode.`);
        // Since we have real navigation, the sockets will be disposed.
        // TODO: Add disposing of sessions.
        this.channel.close(
            // SshDisconnectReason.byApplication,
            'Workbench requested to close the connection.'
        );
    }

    constructor(
        private url: string,
        private accessToken: string,
        private environmentInfo: ICloudEnvironment
    ) {
        this.id = VSLSWebSocket.socketCnt++;
        this.initializeChannel(url);
    }

    private async initializeChannel(url: string, retry = 3) {
        url = url.replace(/skipWebSocketFrames=false/gim, 'skipWebSocketFrames=true');

        let disposables = [];

        try {
            await envConnector.ensureConnection(this.environmentInfo, this.accessToken);

            const channel = await envConnector.sendHandshakeRequest(
                this.createHandshakeRequest(url)
            );
            disposables.push(channel);

            disposables.push(
                channel.onDataReceived((data: Buffer) => {
                    verbose(`[${this.getWebSocketIdentifier()}] SSh channel received data.`);
                    logContent(`[${this.getWebSocketIdentifier()}]\n\n${data.toString()}`);
                    channel.adjustWindow(data.length);

                    this._onData.fire(bufferToArrayBuffer(data));
                })
            );

            disposables.push(
                channel.onClosed(async () => {
                    verbose(`[${this.getWebSocketIdentifier()}] Ssh channel closed.`);

                    this._onClose.fire();
                })
            );

            verbose(`[${this.getWebSocketIdentifier()}] Ssh channel open.`);
            this._onOpen.fire();
            this.channel = channel;
        } catch (err) {
            error(`[${this.getWebSocketIdentifier()}] Ssh channel failed to open.`);
            if (retry <= 0) {
                this._onError.fire(err);

                return;
            }

            envConnector.cleanCachedConnection();
            disposables.forEach((d) => d.dispose());
            disposables = [];

            this.initializeChannel(url, retry - 1);
        }
    }

    private createNonce() {
        const buffer = Buffer.alloc(16);
        for (let i = 0; i < 16; i++) {
            // This matches vscode expectations and isn't meant for security purposes.
            // tslint:disable-next-line: insecure-random
            buffer[i] = Math.round(Math.random() * 256);
        }
        const nonce = buffer.toString('base64');

        return nonce;
    }

    private createHandshakeRequest(url: string) {
        const delimiter = '\r\n';
        const requestArray = [
            `GET ${url} HTTP/1.1`,
            `Connection: Upgrade`,
            `Upgrade: websocket`,
            `Sec-WebSocket-Key: ${this.createNonce()}`,
            delimiter,
        ];
        const requestString = requestArray.join(delimiter);

        return requestString;
    }
}
