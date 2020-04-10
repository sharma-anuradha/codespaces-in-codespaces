import { Disposable } from 'vscode-jsonrpc';

import { ILiveShareClient, IWorkspaceAccess, IWorkspaceInfo, wait } from 'vso-client-core';

import { RequestStore } from './RequestStore';
import { tryAuthenticateMessageType } from './service-worker/service-worker-messages';
import { broadcast } from './service-worker/lib/post-message-utils';
import { createLogger } from './service-worker/lib/logger';

const trace = createLogger('InMemoryLiveShareClient');

export class InMemoryLiveShareClient implements ILiveShareClient, Disposable {
    private readonly workspaceAccessRequests = new RequestStore<IWorkspaceAccess | null>({
        defaultTimeout: 60 * 1000,
    });
    private readonly workspaceInfoRequests = new RequestStore<IWorkspaceInfo | null>({
        defaultTimeout: 60 * 1000,
    });
    private readonly requestedDetails: Set<string> = new Set();

    setWorkspaceInfo(invitationId: string, info: IWorkspaceInfo) {
        invitationId = invitationId.toUpperCase();

        trace.verbose('setWorkspaceInfo', { invitationId });
        this.workspaceInfoRequests.setResponse(invitationId, info);
    }

    getWorkspaceInfo(invitationId: string): Promise<IWorkspaceInfo | null> {
        invitationId = invitationId.toUpperCase();

        // In case this is the first time we are requesting access to this session
        // there's a good chance the service worker has been woken up
        // and should pick up all the credentials from its active clients.
        if (!this.requestedDetails.has(invitationId)) {
            this.requestedDetails.add(invitationId);

            broadcast({
                type: tryAuthenticateMessageType,
            });
        }

        trace.verbose('getWorkspaceInfo', { invitationId });

        wait(30 * 1000).then(() => {
            this.workspaceInfoRequests.setResponse(invitationId, null);
        });

        return this.workspaceInfoRequests.getResponse(invitationId);
    }

    setWorkspaceAccess(workspaceId: string, access: IWorkspaceAccess) {
        workspaceId = workspaceId.toUpperCase();

        trace.verbose('setWorkspaceAccess', { workspaceId });
        this.workspaceAccessRequests.setResponse(workspaceId, access);
    }

    getWorkspaceAccess(workspaceId: string): Promise<IWorkspaceAccess | null> {
        workspaceId = workspaceId.toUpperCase();

        trace.verbose('getWorkspaceAccess', { workspaceId });

        wait(30 * 1000).then(() => {
            this.workspaceAccessRequests.setResponse(workspaceId, null);
        });

        return this.workspaceAccessRequests.getResponse(workspaceId);
    }

    dispose() {
        this.workspaceAccessRequests.dispose();
        this.workspaceInfoRequests.dispose();
    }
}
