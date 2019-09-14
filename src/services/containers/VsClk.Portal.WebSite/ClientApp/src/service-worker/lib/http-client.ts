import parseMessage from 'http-message-parser';

import { ConnectionManager } from './connection-manager';
import { parseRequestUrl, ParsedAssetRequestUrl } from '../../common/vscode-url-utils';
import { Signal } from '../../utils/signal';
import { createLogger, Logger } from './logger';
import { createUniqueId } from '../../dependencies';

const separator = '\r\n';
const [crByte, lfByte] = Array.from(new TextEncoder().encode(separator));

// tslint:disable-next-line: export-name
export class LiveShareHttpClient {
    private readonly logger: Logger;

    constructor(private readonly connectionManager: ConnectionManager) {
        this.logger = createLogger('LiveShareHttpClient');
    }

    async fetch(request: Request, preloadResponse?: Promise<any>): Promise<Response> {
        const parsedUrl = parseRequestUrl(request.url);

        if (!parsedUrl.isAssetUrl) {
            return preloadResponse || this.respondWithDefault(request);
        }

        this.logger.verbose('Fetching.', parsedUrl);
        return this.respondWithAssetFromEnvironment(request, parsedUrl);
    }

    private async respondWithDefault(request: Request): Promise<Response> {
        if (request.cache === 'only-if-cached' && request.mode !== 'same-origin') {
            // https://bugs.chromium.org/p/chromium/issues/detail?id=823392
            // https://stackoverflow.com/questions/48463483/what-causes-a-failed-to-execute-fetch-on-serviceworkerglobalscope-only-if#49719964
            // https://developer.mozilla.org/en-US/docs/Web/API/Request/cache
            return new Response(undefined, {
                status: 504,
                statusText:
                    'Gateway Timeout (dev tools: https://bugs.chromium.org/p/chromium/issues/detail?id=823392)',
            });
        }
        return await fetch(request);
    }

    private async respondWithAssetFromEnvironment(
        request: Request,
        parsedUrl: ParsedAssetRequestUrl
    ): Promise<Response> {
        try {
            const requestId = createUniqueId();
            const channel = await this.connectionManager.getChannelFor(
                {
                    requestId,
                    sessionId: parsedUrl.sessionId,
                },
                parsedUrl.port
            );

            const requestString = this.createHttpRequest(request, parsedUrl);
            this.logger.verbose('Sending request', {
                requestId,
                ...parsedUrl,
                request: requestString,
            });
            channel.send(Buffer.from(requestString));

            return await this.parseResponseFrom(parsedUrl, channel);
        } catch (error) {
            this.logger.error('Failed to respond with asset', {
                ...parsedUrl,
                error,
            });
        }

        return new Response(undefined, {
            status: 502,
            statusText: 'Request through port forwarding failed.',
        });
    }

    // tslint:disable-next-line: max-func-body-length
    parseResponseFrom(
        url: ParsedAssetRequestUrl,
        channel: import('@vs/vs-ssh').SshChannel
    ): Promise<Response> {
        let headerString: string | undefined;
        let responseContent: Buffer;
        let responseMetadata: ReturnType<typeof parseMessage> | undefined;
        const responseSignal = new Signal<Response>();

        channel.onDataReceived(async (buffer) => {
            // 1. First chunk will contain (Request Line followed by headers, each separated by CRLF) and separated from body with CRLF
            // e.g.
            //
            //      HTTP/1.1 200 OK
            //      Cache-Control: public, max-age=31536000
            //      X-VSCode-Extension: true
            //      Content-Type: image/svg+xml
            //      Date: Wed, 04 Sep 2019 00:34:55 GMT
            //      Connection: close
            //      Transfer-Encoding: chunked
            //

            let chunk = buffer;

            if (!headerString) {
                // 2. Find first empty line separating metadata from body (2x CRLF or bytes 13, 10, 13, 10)
                const headerStringEndIndex = chunk.findIndex(
                    (_, index) =>
                        chunk[index + 0] === crByte &&
                        chunk[index + 1] === lfByte &&
                        chunk[index + 2] === crByte &&
                        chunk[index + 3] === lfByte
                );
                const bodyStartIndex = headerStringEndIndex + 4;

                headerString = chunk.slice(0, headerStringEndIndex).toString();
                responseMetadata = parseMessage(headerString);

                chunk = chunk.slice(bodyStartIndex);
            }

            responseContent = responseContent
                ? this.concatBuffers(responseContent, chunk)
                : this.cloneBuffer(chunk);

            channel.adjustWindow(responseContent.length);
        });

        channel.onClosed(async (e) => {
            if (e.error) {
                this.logger.error('Channel closed with error', {
                    error: e.error,
                    errorMessage: e.errorMessage,
                    url: url.href,
                });

                responseSignal.reject(e.error);
                return;
            }

            if (!responseMetadata) {
                this.logger.error('Failed to parse headers from the request.', {
                    url: url.href,
                });

                responseSignal.complete(
                    new Response(undefined, {
                        status: 504,
                        statusText:
                            'Port forwarding channel closed before response properly handled.',
                    })
                );
                return;
            }

            let body: Buffer | undefined = responseContent;

            if (
                responseMetadata.headers &&
                responseMetadata.headers['Transfer-Encoding'] &&
                responseMetadata.headers['Transfer-Encoding'].includes('chunked')
            ) {
                try {
                    body = this.removeChunkFrames(body);
                } catch (error) {
                    this.logger.error('Failed to parse chunked body.', {
                        error,
                        url: url.href,
                    });

                    responseSignal.complete(
                        new Response(undefined, {
                            status: 502,
                            statusText: 'Port forwarding failed to parse response from server.',
                        })
                    );
                    return;
                }
            }

            if (responseMetadata.headers) {
                responseMetadata.headers['X-Powered-By'] = 'Visual Studio Online Service Worker';
            } else {
                responseMetadata.headers = {};
                responseMetadata.headers['X-Powered-By'] = 'Visual Studio Online Service Worker';
            }

            try {
                const response = new Response(body, {
                    headers: responseMetadata.headers,
                    status: responseMetadata.statusCode,
                    statusText: responseMetadata.statusMessage,
                });

                responseSignal.complete(response);
            } catch (error) {
                this.logger.error('Failed to create response from received data.', {
                    ...responseMetadata,
                    body,
                    url: url.href,
                });

                responseSignal.complete(
                    new Response(undefined, {
                        status: 502,
                        statusText: `Port forwarding failed to parse response from server. (${error.message})`,
                    })
                );
            }
        });

        return responseSignal.promise;
    }

    createHttpRequest(request: Request, parsedUrl: ParsedAssetRequestUrl): string {
        const headers: string[] = [];
        for (const [header, value] of request.headers.entries()) {
            headers.push(`${header}: ${value}`);
        }

        // Add connection token as header since the server expects it to be there.
        headers.push(`vscode-tkn: ${parsedUrl.tkn}`);

        const httpStringRows: string[] = [
            `${request.method} ${parsedUrl.vscodeServerEndpoint} HTTP/1.1`,
            `Host: localhost`,
            `Connection: ${request.keepalive ? 'keep-alive' : 'close'}`,
            ...headers,
            separator,
        ];

        return httpStringRows.join(separator);
    }

    private concatBuffers(...buffers: Buffer[]) {
        const finalLength = buffers.reduce((length, buffer) => length + buffer.byteLength, 0);

        const concatenatedBuffer = new ArrayBuffer(finalLength);
        const typedView = new Uint8Array(concatenatedBuffer);

        typedView.set(buffers[0], 0);
        for (let i = 1; i < buffers.length; i++) {
            typedView.set(buffers[i], buffers[i - 1].byteLength);
        }

        return Buffer.from(concatenatedBuffer);
    }

    private cloneBuffer(buffer: Buffer) {
        return Buffer.from(buffer);
    }

    private removeChunkFrames(body: Buffer): Buffer | undefined {
        // body contains concatenated chunked content
        const [chunk, rest] = stripFrameFromFirstChunk(body);

        if (chunk && rest) {
            const newChunk = this.removeChunkFrames(rest);

            if (newChunk) {
                // TODO: #982901 - Handle endianness of container VSCode server responses properly.
                return this.concatBuffers(chunk, newChunk);
            }
        }

        if (chunk) {
            return chunk;
        }

        return undefined;

        function stripFrameFromFirstChunk(partialBodyContent: Buffer) {
            let chunk = partialBodyContent;

            // every chunk starts with its length in hex followed by CRLF and ends with CRLF
            const endOfLengthHeader = indexOfCRLF(chunk);

            if (endOfLengthHeader < 0) {
                throw new Error('Trying to parse invalid content');
            }

            const chunkLength = Number.parseInt(chunk.slice(0, endOfLengthHeader).toString(), 16);

            if (chunkLength === 0) {
                return [];
            }

            chunk = chunk.slice(endOfLengthHeader + 2);

            return [chunk.slice(0, chunkLength), chunk.slice(chunkLength + 2)];
        }

        function indexOfCRLF(buffer: Buffer, fromIndex: number = 0) {
            const crIndex = buffer.indexOf(crByte, fromIndex);
            if (crIndex < 0) {
                return -1;
            }

            const lfIndex = buffer.indexOf(lfByte, crIndex);
            if (lfIndex !== crIndex + 1) {
                return -1;
            }

            return crIndex;
        }
    }
}
