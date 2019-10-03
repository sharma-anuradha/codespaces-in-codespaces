export interface IWorkspaceInfo {
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

export interface IWorkspaceAccess {
    sessionToken: string;
    relaySas: string;
}

export interface ILiveShareClient {
    getWorkspaceInfo(invitationId: string): Promise<IWorkspaceInfo | null>;
    getWorkspaceAccess(workspaceId: string): Promise<IWorkspaceAccess | null>;
}
