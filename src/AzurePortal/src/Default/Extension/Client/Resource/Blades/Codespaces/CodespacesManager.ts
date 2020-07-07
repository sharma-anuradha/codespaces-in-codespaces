import { Codespace, Location } from './CodespaceModels';

export interface CodespacesManager {
    fetchLocation(id?: string): Q.Promise<Location>;

    createCodespace(
        codespace: Omit<Codespace, 'type' | 'id' | 'state' | 'planId'>
    ): Q.Promise<Codespace>;
    fetchCodespaces(): Q.Promise<Codespace[]>;
    fetchCodespace(id: string): Q.Promise<Codespace>;
    suspendCodespace(id: string): Q.Promise<void>;
    pollTransitioningCodespace(id: string): Q.Promise<boolean>;
    deleteCodespace(id: string): Q.Promise<void>;
    editCodespace(id: string, skuName: string, autoShutdownDelayMinutes: number): Q.Promise<void>;
}
