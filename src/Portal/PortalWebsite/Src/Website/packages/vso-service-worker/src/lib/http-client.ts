import parseMessage from 'http-message-parser';
import { SshChannel } from '@vs/vs-ssh';

import { ConnectionManager } from './connection-manager';
import { getRoutingDetails, RoutingDetails } from './url-utils';
import { createLogger, Logger } from './logger';
import { CriticalError } from './errors/CriticalError';
import { Signal, randomString } from 'vso-client-core';

const separator = '\r\n';
const [crByte, lfByte] = Array.from(new TextEncoder().encode(separator));

const serviceRequestUrls = [
    '/authenticate-port-forwarder',
    '/logout-port-forwarder'
];

export interface IHttpClient {
    fetch(request: Request, preloadResponse?: Promise<any>): Promise<Response>;
}

export class PassThroughHttpClient implements IHttpClient {
    async fetch(request: Request): Promise<Response> {
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
}

export class LiveShareHttpClient implements IHttpClient {
    private readonly logger: Logger;
    private readonly basicHttpClient = new PassThroughHttpClient();

    constructor(private readonly connectionManager: ConnectionManager) {
        this.logger = createLogger('LiveShareHttpClient');
    }

    async fetch(request: Request): Promise<Response> {
        const routingDetails = getRoutingDetails(request.url);

        if (!routingDetails || this.shouldPassThroughRequest(request)) {
            return this.basicHttpClient.fetch(request);
        }

        if (!routingDetails.shouldCacheResponse) {
            return await this.respondWithAssetFromEnvironment(request, routingDetails);
        }

        try {
            const cache = await caches.open(routingDetails.sessionId);
            const cachedResource = await cache.match(request);
            if (cachedResource) {
                this.logger.verbose('Returning cached resource.');
                return cachedResource;
            }
        } catch (error) {
            this.logger.error('Failed to get resource from cache.', { error });
        }

        const response = await this.respondWithAssetFromEnvironment(request, routingDetails);

        if (response.ok) {
            try {
                const cache = await caches.open(routingDetails.sessionId);
                await cache.put(request, response.clone());
            } catch (error) {
                this.logger.error('Failed to store resource to cache.', { error });
            }
        }

        return response;
    }

    private async respondWithAssetFromEnvironment(
        request: Request,
        routingDetails: RoutingDetails
    ): Promise<Response> {
        try {
            const requestId = randomString();
            const channel = await this.connectionManager.getChannelFor(
                {
                    requestId,
                    sessionId: routingDetails.sessionId,
                },
                routingDetails.port
            );

            const requestString = this.createHttpRequest(request, routingDetails.containerUrl);

            this.logger.verbose('Sending request', {
                requestId,
                ...routingDetails,
                request: requestString,
            });
            channel.send(Buffer.from(requestString));

            return await this.parseResponseFrom(routingDetails.containerUrl, channel);
        } catch (error) {
            this.logger.error('Failed to respond with asset', {
                ...routingDetails,
                error,
            });

            if (error instanceof CriticalError) {
                throw error;
            }
        }

        return new Response(undefined, {
            status: 502,
            statusText: 'Request through port forwarding failed.',
        });
    }

    // tslint:disable-next-line: max-func-body-length
    parseResponseFrom(url: URL, channel: SshChannel): Promise<Response> {
        let responseBuffer = new Buffer(0);
        let headerString: string | undefined;
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

            const chunkLength = buffer.byteLength;
            responseBuffer = this.concatBuffers(responseBuffer, buffer);

            if (!headerString) {
                // 2. Find first empty line separating metadata from body (2x CRLF or bytes 13, 10, 13, 10)
                const headerStringEndIndex = responseBuffer.findIndex(
                    (_, index) =>
                        responseBuffer[index + 0] === crByte &&
                        responseBuffer[index + 1] === lfByte &&
                        responseBuffer[index + 2] === crByte &&
                        responseBuffer[index + 3] === lfByte
                );

                if (headerStringEndIndex > 0) {
                    headerString = responseBuffer.slice(0, headerStringEndIndex).toString();

                    const bodyStartIndex = headerStringEndIndex + 4;
                    responseBuffer = responseBuffer.slice(bodyStartIndex);
                }
            }

            channel.adjustWindow(chunkLength);
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

            if (!headerString) {
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

            const responseMetadata = parseMessage(headerString);

            let body: Buffer | undefined = responseBuffer;

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
                responseMetadata.headers['X-Powered-By'] = 'Visual Studio Codespaces Service Worker';
            } else {
                responseMetadata.headers = {};
                responseMetadata.headers['X-Powered-By'] = 'Visual Studio Codespaces Service Worker';
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

    createHttpRequest(request: Request, parsedUrl: URL, connectionToken?: string): string {
        const headers: string[] = [];

        request.headers.forEach((value, header) => {
            headers.push(`${header}: ${value}`);
        });

        if (connectionToken) {
            // Add connection token as header since the server expects it to be there.
            headers.push(`vscode-tkn: ${connectionToken}`);
        }

        const httpStringRows: string[] = [
            `${request.method} ${parsedUrl.pathname + parsedUrl.search + parsedUrl.hash} HTTP/1.1`,
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

    private shouldPassThroughRequest(request: Request): boolean {
        const url = new URL(request.url);

        return serviceRequestUrls.includes(url.pathname.toLowerCase());
    }
}
