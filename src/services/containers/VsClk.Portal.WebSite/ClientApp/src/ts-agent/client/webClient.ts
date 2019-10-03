import { trace } from '../../utils/trace';
import { IWorkspaceAccess, IWorkspaceInfo, ILiveShareClient } from './ILiveShareClient';
import { ICredentialsProvider } from './ICredentialsProvider';

export interface AuthToken {
    access_token: string;
    refresh_token?: string;
}

export class WebClient implements ILiveShareClient {
    private readonly baseUri: string;

    public constructor(
        public readonly serviceUri: string,
        private readonly credentialsProvider: ICredentialsProvider
    ) {
        this.baseUri = serviceUri + '/api/v1.2';
    }

    private getRequestHeaders(sessionId: string) {
        return {
            'Cache-Control': 'no-cache',
            'Content-Type': 'application/json',
            Authorization: `Bearer ${this.credentialsProvider.getToken(sessionId)}`,
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

    public async getWorkspaceInfo(invitationId: string): Promise<IWorkspaceInfo | null> {
        trace(`${this.baseUri}/workspace/${invitationId}`);
        const response = await fetch(`${this.baseUri}/workspace/${invitationId}`, {
            method: 'GET',
            headers: this.getRequestHeaders(invitationId),
        });
        return await this.parseResponse<IWorkspaceInfo>(response, `GET workspace/${invitationId}`);
    }

    public async getWorkspaceAccess(workspaceId: string): Promise<IWorkspaceAccess | null> {
        const response = await fetch(`${this.baseUri}/workspace/${workspaceId}/user`, {
            method: 'PUT',
            headers: this.getRequestHeaders(workspaceId),
        });
        return await this.parseResponse<IWorkspaceAccess>(
            response,
            `PUT workspace/${workspaceId}/user`
        );
    }
}
