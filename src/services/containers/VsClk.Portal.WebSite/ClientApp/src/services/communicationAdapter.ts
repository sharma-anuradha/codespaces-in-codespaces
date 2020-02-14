import * as vsls from '../ts-agent/contracts/VSLS';
import { authService } from '../services/authService';
import { EnvConnector } from '../ts-agent/envConnector';
import { WorkspaceClient } from '../ts-agent/workspaceClient';
import { openSshChannel } from '../ts-agent/openSshChannel';
import { SplashCommunicationProvider } from '../providers/splashCommunicationProvider';
import { trace } from '../utils/trace';

export class CommunicationAdapter {
    private envConnector: EnvConnector;
    private communicationProvider: SplashCommunicationProvider;
    private utf8Decoder: TextDecoder;
    private correlationId: string;
    private liveShareEndpoint: string;

    constructor(communication: SplashCommunicationProvider, liveShareEndpoint: string, correlationId: string) {
        this.envConnector = new EnvConnector();
        this.communicationProvider = communication;
        this.utf8Decoder = new TextDecoder('utf-8');
        this.correlationId = correlationId;
        this.liveShareEndpoint = liveShareEndpoint;
    }

    public async connect(workspaceId: string) {
        this.communicationProvider.updateStep({
            name: 'containerSetup',
            status: 'running',
        });
        const token = await authService.getCachedToken();
        const workspaceClient = await this.envConnector.connectWithRetry(
            workspaceId,
            token!.accessToken,
            this.liveShareEndpoint,
            this.correlationId,
        );

        if (workspaceClient && workspaceClient.sshSession) {
            this.startStreamingTerminal(workspaceClient);
        } else {
            trace('Workspace client did not initialize correctly');
            this.communicationProvider.updateStep({
                name: 'containerSetup',
                status: 'failed',
            });
        }
    }

    public async startStreamingTerminal(workspaceClient: WorkspaceClient) {
        const terminalClient = workspaceClient.getServiceProxy<vsls.TerminalService>(
            vsls.TerminalService
        );
        const streamManagerClient = workspaceClient.getServiceProxy<vsls.StreamManagerService>(
            vsls.StreamManagerService
        );

        let runningTerminals: vsls.TerminalInfo[] | undefined;
        try {
            runningTerminals = await terminalClient!.getRunningTerminalsAsync();
        } catch (error) {
            trace('No running terminals found');
            this.communicationProvider.updateStep({
                name: 'containerSetup',
                status: 'failed',
            });
        }

        if (runningTerminals && runningTerminals.length > 0) {
            try {
                const streamId = await streamManagerClient!.getStreamAsync(
                    runningTerminals[0].streamName,
                    runningTerminals[0].streamCondition,
                );
                const channel = await openSshChannel(workspaceClient.sshSession!, streamId);
                channel.onDataReceived((data: any) => {
                    this.communicationProvider.writeToTerminalOutput(this.utf8Decoder.decode(data));
                });
                channel.onClosed(() => {
                    trace('Channel closed');
                    this.communicationProvider.updateStep({
                        name: 'containerSetup',
                        status: 'completed',
                    });
                });
            } catch (e) {
                trace('Exception on ssh communication');
                this.communicationProvider.updateStep({
                    name: 'containerSetup',
                    status: 'failed',
                });
            }
        }
    }
}
