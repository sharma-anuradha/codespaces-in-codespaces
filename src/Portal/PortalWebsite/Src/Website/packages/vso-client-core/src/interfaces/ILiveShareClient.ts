export interface IWorkspaceInfo {
    readonly id: string;
    readonly name: string;
    readonly ownerId: string;
    readonly joinLink: string;
    readonly invitationLinks?: string[];
    readonly connectLinks: string[];
    readonly relayLink?: string;
    readonly relaySas?: string;
    readonly hostPublicKeys: string[];
    readonly conversationId: string;
}

export interface IWorkspaceAccess {
    readonly sessionToken: string;
    readonly relaySas: string;
}

export interface ILiveShareClient {
    getWorkspaceInfo(invitationId: string): Promise<IWorkspaceInfo | null>;
    getWorkspaceAccess(workspaceId: string): Promise<IWorkspaceAccess | null>;
}
