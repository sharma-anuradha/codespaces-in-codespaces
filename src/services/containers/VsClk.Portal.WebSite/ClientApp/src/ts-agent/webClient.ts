import * as ssh from '@vs/vs-ssh';
import { trace } from '../utils/trace';

export interface AuthToken {
    access_token: string;
    refresh_token?: string;
}

export interface WorkspaceInfo {
    id: string;
    name: string;
    ownerId: string;
    joinLink: string;
    connectLinks: string[];
    relayLink?: string;
    relaySas?: string;
    hostPublicKeys: string[];
    conversationId: string;
}

export interface WorkspaceAccess {
    sessionToken: string;
    relaySas: string;
}

export class WebClient {
    private readonly baseUri: string;

    public constructor(public readonly serviceUri: string, private readonly authToken: string) {
        this.baseUri = serviceUri + '/api/v1.2';
    }

    private get requestHeaders() {
        return {
            'Cache-Control': 'no-cache',
            'Content-Type': 'application/json',
            Authorization: `Bearer ${this.authToken}`,
        };
    }

    private async parseResponse<T>(response: Response, description: string): Promise<T | null> {
        if (response.ok) {
            const result: T = await response.json();
            trace(`${description} => ${JSON.stringify(result)}`);
            return result;
        } else if (response.status === 404) {
            trace(`${description} => null`);
            return null;
        } else {
            throw new Error(`${description} => status: ${response.status}`);
        }
    }

    public async getWorkspaceInfo(invitationId: string): Promise<WorkspaceInfo | null> {
        trace(`${this.baseUri}/workspace/${invitationId}`);
        const response = await fetch(`${this.baseUri}/workspace/${invitationId}`, {
            method: 'GET',
            headers: this.requestHeaders,
        });
        return await this.parseResponse<WorkspaceInfo>(response, `GET workspace/${invitationId}`);
    }

    public async getWorkspaceAccess(workspaceId: string): Promise<WorkspaceAccess | null> {
        const response = await fetch(`${this.baseUri}/workspace/${workspaceId}/user`, {
            method: 'PUT',
            headers: this.requestHeaders,
        });
        return await this.parseResponse<WorkspaceAccess>(
            response,
            `PUT workspace/${workspaceId}/user`,
        );
    }

    public openConnection(workspace: WorkspaceInfo): Promise<ssh.Stream> {
        if (!workspace.relayLink) {
            throw new Error('Workspace does not have a relay endpoint.');
        }

        // Reference:
        // https://github.com/Azure/azure-relay-node/blob/7b57225365df3010163bf4b9e640868a02737eb6/hyco-ws/index.js#L107-L137
        const relayUri =
            workspace.relayLink.replace('sb:', 'wss:').replace('.net/', '.net:443/$hc/') +
            '?sb-hc-action=connect&sb-hc-token=' +
            encodeURIComponent(workspace.relaySas || '');

        // There are two relay websocket implementations below:
        //   1) Using the browser (W3C) websocket API adapter provided by the node-websocket package.
        //      This code is kept for future compatibility with browser (VS Online) clients.
        //   2) Using the node-websocket API directly
        //      This enables better error diagnostic information and therefore is preferred.
        const socket = new WebSocket(relayUri);
        socket.binaryType = 'arraybuffer';
        return new Promise<ssh.Stream>((resolve, reject) => {
            socket.onopen = () => {
                resolve(new ssh.WebSocketStream(socket));
            };
            socket.onerror = (e) => {
                reject(new Error('Failed to connect to relay: ' + relayUri));
            };
        });

        // const client = new WebSocketWebSocket();
        // return new Promise<ssh.Stream>((resolve, reject) => {
        //     client.on('connect', (connection) => {
        //         resolve(new ssh.WebSocketStream(new WebsocketStreamAdapter(connection)));
        //     });
        //     client.on('connectFailed', (e) => {
        //         if (e.message && e.message.startsWith('Error: ')) {
        //             e.message = e.message.substr(7);
        //         }

        //         // Unfortunately the status code can only be obtained from the error message.
        //         // Also status 404 may be used for at least two distinct error conditions.
        //         // So we have to match on the error message text. This could break when
        //         // the relay server behavior changes or when updating the client websocket library.
        //         // But then in the worst case the original error message will be reported.
        //         if (/status: 401/.test(e.message)) {
        //             e.message = 'Relay SAS token is invalid.';
        //         } else if (/status: 404 Endpoint does not exist/.test(e.message)) {
        //             e.message = 'Relay endpoint was not found.';
        //         } else if (/status: 404 There are no listeners connected/.test(e.message)) {
        //             e.message = 'Relay listener offline.';
        //         } else if (/status: 500/.test(e.message)) {
        //             e.message = 'Relay server error.';
        //         } else {
        //             // Other errors are most likely connectivity issues.
        //             // The original error message may have additional helpful details.
        //             e.message = 'Relay connection error' + ' ' + e.message;
        //         }

        //         reject(e);
        //     });
        //     client.connect(relayUri);
        // });
    }
}

/**
 * Partially adapts a Node websocket connection object to the browser websocket API,
 * enough so that it can be used as an SSH stream.
 */
class WebsocketStreamAdapter {
    constructor(private connection: any) {}

    set onmessage(messageHandler: ((e: { data: ArrayBuffer }) => void) | null) {
        if (messageHandler) {
            this.connection.on('message', (message: any) => {
                // This assumes all messages are binary.
                messageHandler({ data: message.binaryData! });
            });
        } else {
            // Removing event handlers is not implemented.
        }
    }

    set onclose(
        closeHandler: ((e: { code?: number; reason?: string; wasClean: boolean }) => void) | null,
    ) {
        if (closeHandler) {
            this.connection.on('close', (code: any, reason: any) => {
                closeHandler({ code, reason, wasClean: !(code || reason) });
            });
        } else {
            // Removing event handlers is not implemented.
        }
    }

    public send(data: ArrayBuffer): void {
        if (Buffer.isBuffer(data)) this.connection.sendBytes(data);
        else this.connection.sendBytes(Buffer.from(data));
    }

    public close(code?: number, reason?: string): void {
        if (code || reason) {
            this.connection.drop(code, reason);
        } else {
            this.connection.close();
        }
    }
}
