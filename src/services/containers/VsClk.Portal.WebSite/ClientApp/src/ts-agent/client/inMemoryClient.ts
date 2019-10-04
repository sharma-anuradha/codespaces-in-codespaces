import { ILiveShareClient, IWorkspaceInfo, IWorkspaceAccess } from './ILiveShareClient';
import { RequestStore } from './RequestStore';
import { Disposable } from 'vscode-jsonrpc';
import { createLogger } from '../../service-worker/lib/logger';
import { wait } from '../../dependencies';

const trace = createLogger('InMemoryLiveShareClient');

export class InMemoryLiveShareClient implements ILiveShareClient, Disposable {
    private readonly workspaceAccessRequests = new RequestStore<IWorkspaceAccess | null>({
        defaultTimeout: 60 * 1000,
    });
    private readonly workspaceInfoRequests = new RequestStore<IWorkspaceInfo | null>({
        defaultTimeout: 60 * 1000,
    });

    setWorkspaceInfo(invitationId: string, info: IWorkspaceInfo) {
        invitationId = invitationId.toUpperCase();

        trace.verbose('setWorkspaceInfo', { invitationId });
        this.workspaceInfoRequests.setResponse(invitationId, info);
    }

    getWorkspaceInfo(invitationId: string): Promise<IWorkspaceInfo | null> {
        invitationId = invitationId.toUpperCase();

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
