import { VSCodeQuality } from './../utils/vscode';
import { SshChannel } from '@vs/vs-ssh';
import { Event, Emitter } from 'vscode-jsonrpc';

import { IWebSocket } from 'vscode-web';

import { EnvConnector } from '../ts-agent/envConnector';
import { bufferToArrayBuffer } from '../utils/bufferToArrayBuffer';
import { trace as baseTrace } from '../utils/trace';
import { createTrace } from 'vso-client-core';

import { ICloudEnvironment, StateInfo } from '../interfaces/cloudenvironment';
import { sendTelemetry } from '../utils/telemetry';
import * as envRegService from '../services/envRegService';

const TRACE_NAME = 'vsls-web-socket';
const { verbose, info, error } = createTrace(TRACE_NAME);

const logContent = baseTrace.extend(`${TRACE_NAME}:trace:content`);
logContent.log =
    // tslint:disable-next-line: no-console
    typeof console.debug === 'function' ? console.debug.bind(console) : console.log.bind(console);

export const envConnector = new EnvConnector();

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

        this.channel.send(bufferData).catch((err) => {
            this._onError.fire(err);
        });
    }

    public close() {
        info(`[${this.getWebSocketIdentifier()}] Ssh channel closed by VSCode.`);

        sendTelemetry('vsonline/portal/ls-connection-close', {
            connectionCorrelationId: this.correlationId,
            isFirstConnection: this.id === 0,
            connectionNumber: this.id,
            environmentType: this.environmentInfo.type,
        });

        // Since we have real navigation, the sockets will be disposed.
        // TODO: Add disposing of sessions.
        this.channel.close(
            // SshDisconnectReason.byApplication,
            'Workbench requested to close the connection.'
        );
    }

    constructor(
        private readonly url: string,
        private readonly accessToken: string,
        private readonly environmentInfo: ICloudEnvironment,
        private readonly liveShareEndpoint: string,
        private readonly correlationId: string,
        private readonly quality: VSCodeQuality
    ) {
        this.id = VSLSWebSocket.socketCnt++;

        this.initializeChannel(url);
    }

    // tslint:disable-next-line: max-func-body-length
    private async initializeChannel(url: string, retry = 3) {
        window.performance.mark(
            `VSLSWebSocket.initializeChannel[${this.id}] - ls-connection-initializing`
        );
        sendTelemetry('vsonline/portal/ls-connection-initializing', {
            connectionCorrelationId: this.correlationId,
            isFirstConnection: this.id === 0,
            connectionNumber: this.id,
            environmentType: this.environmentInfo.type,
        });

        url = url.replace(/skipWebSocketFrames=false/gim, 'skipWebSocketFrames=true');

        let disposables = [];

        const timeout = new Promise((_, reject) => {
            // tslint:disable-next-line: no-string-based-set-timeout
            setTimeout(reject, 20 * 1000, new Error('VSLSSocketTimeout'));
        });

        const environmentCheck = envRegService.getEnvironment(this.environmentInfo.id).then(
            (environment) => {
                if (environment && environment.state !== StateInfo.Available) {
                    throw new Error('EnvironmentNotAvailable');
                }
            },
            (err) => {
                // noop
            }
        );

        // In case the environment is suspended, we want to know as soon as possible in the process.
        //
        // We don't want to delay the connection/reconnection by the time
        // it takes to check the environment status.
        //
        // Both timeout and environment check reject and send us on error handling path
        // of connection process.
        // In case of failure, we are interested in the earliest failure.
        // In case successful environment check we want to keep watching for the timeout.
        const combinedTimeoutEnvCheck = Promise.race([timeout, environmentCheck]).then(
            () => timeout
        );

        try {
            await Promise.race([
                envConnector.ensureConnection(
                    this.environmentInfo,
                    this.accessToken,
                    this.liveShareEndpoint,
                    this.quality
                ),
                combinedTimeoutEnvCheck,
            ]);

            const channel = await Promise.race([
                envConnector.sendHandshakeRequest(this.createHandshakeRequest(url)),
                combinedTimeoutEnvCheck as Promise<SshChannel>,
            ]);
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

            window.performance.measure(
                `VSLSWebSocket.initializeChannel[${this.id}] - ls-connection-opened`,
                `VSLSWebSocket.initializeChannel[${this.id}] - ls-connection-initializing`
            );
            const [measure] = window.performance.getEntriesByName(
                `VSLSWebSocket.initializeChannel[${this.id}] - ls-connection-opened`
            );
            sendTelemetry('vsonline/portal/ls-connection-opened', {
                connectionCorrelationId: this.correlationId,
                isFirstConnection: this.id === 0,
                connectionNumber: this.id,
                environmentType: this.environmentInfo.type,
                duration: measure.duration,
            });

            this.channel = channel;
        } catch (err) {
            error(`[${this.getWebSocketIdentifier()}] Ssh channel failed to open.`);

            window.performance.measure(
                `VSLSWebSocket.initializeChannel[${this.id}] - ls-connection-failed`,
                `VSLSWebSocket.initializeChannel[${this.id}] - ls-connection-initializing`
            );
            const [measure] = window.performance.getEntriesByName(
                `VSLSWebSocket.initializeChannel[${this.id}] - ls-connection-failed`
            );
            sendTelemetry('vsonline/portal/ls-connection-failed', {
                connectionCorrelationId: this.correlationId,
                isFirstConnection: this.id === 0,
                connectionNumber: this.id,
                environmentType: this.environmentInfo.type,
                duration: measure.duration,
                retry,
                error: err,
            });

            if (retry <= 0) {
                this._onError.fire(err);

                return;
            }

            if (err.message === 'EnvironmentNotAvailable') {
                sendTelemetry('vsonline/portal/ls-connection-page-reload', {
                    connectionCorrelationId: this.correlationId,
                    isFirstConnection: this.id === 0,
                    connectionNumber: this.id,
                    environmentType: this.environmentInfo.type,
                    retry,
                });

                const currentUrl = new URL(window.location.href);
                currentUrl.searchParams.set('autoStart', 'false');

                window.location.replace(currentUrl.toString());
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
