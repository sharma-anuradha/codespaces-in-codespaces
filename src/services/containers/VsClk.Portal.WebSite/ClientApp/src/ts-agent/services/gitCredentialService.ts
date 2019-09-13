import * as vsls from '../contracts/VSLS';
import * as rpc from 'vscode-jsonrpc';
import { SharedServiceImp } from './sharedService';

const serviceName = 'IGitCredentialManager';
const credentialFunction = 'credentialFill';

export class GitCredentialService {
    private workspaceService: vsls.WorkspaceService;
    private rpcConnection: rpc.MessageConnection;
    private sharedService?: SharedServiceImp;

    constructor(service: vsls.WorkspaceService, connection: rpc.MessageConnection) {
        this.workspaceService = service;
        this.rpcConnection = connection;
    }

    public async shareService(): Promise<void> {
        this.sharedService = new SharedServiceImp(serviceName, this.rpcConnection);

        await this.workspaceService.registerServicesAsync(
            [serviceName],
            vsls.CollectionChangeType.Add
        );

        this.sharedService.onRequest(credentialFunction, async () => {
            return '';
        });
    }
}
