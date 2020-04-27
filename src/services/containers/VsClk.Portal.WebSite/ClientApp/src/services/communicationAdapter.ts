import {
    createTrace,
    vsls
} from 'vso-client-core';
import {
    EnvConnector,
    WorkspaceClient,
    openSshChannel
} from 'vso-ts-agent';

import { SplashCommunicationProvider } from '../providers/splashCommunicationProvider';
import { useActionContext } from '../actions/middleware/useActionContext';
import { TerminalId } from '../constants';
import { BrowserSyncService } from '../rpcServices/BrowserSyncService';
import { GitCredentialService } from '../rpcServices/GitCredentialService';

export class CommunicationAdapter {
    private envConnector: EnvConnector;
    private communicationProvider: SplashCommunicationProvider;
    private utf8Decoder: TextDecoder;
    private correlationId: string;
    private liveShareEndpoint: string;
    private stepIdentifiers: { [key: string]: RegExp } = {};
    private logger: {
        verbose: debug.Debugger,
        info: debug.Debugger,
        warn: debug.Debugger,
        error: debug.Debugger,
    };

    constructor(communication: SplashCommunicationProvider, liveShareEndpoint: string, correlationId: string) {
        this.envConnector = new EnvConnector(async (e) => {
            const {
                workspaceClient,
                workspaceService,
                rpcConnection
            } = e;

            // Expose credential service
            const gitCredentialService = new GitCredentialService(workspaceService, rpcConnection);
            await gitCredentialService.shareService();

            // Expose browser sync service
            const sourceEventService = workspaceClient.getServiceProxy<vsls.SourceEventService>(
                vsls.SourceEventService
            );
            
            new BrowserSyncService(sourceEventService);
        });
        this.communicationProvider = communication;
        this.utf8Decoder = new TextDecoder('utf-8');
        this.correlationId = correlationId;
        this.liveShareEndpoint = liveShareEndpoint;
        this.logger = createTrace('Communication Adapter');
    }

    public async connect(sessionId: string) {
        const { state } = useActionContext();
        const { authentication } = state;
        const { token } = authentication;

        if (!token) {
            throw new Error('Not authorized.');
        }
            
        const workspaceClient = await this.envConnector.connectWithRetry(
            sessionId,
            token,
            this.liveShareEndpoint,
            this.correlationId,
        );

        if (workspaceClient && workspaceClient.sshSession) {
            this.startStreamingTerminal(workspaceClient);
        } else {
            this.logger.error('Workspace client did not initialize correctly');
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
            this.logger.error('No running terminals found');
        }

        if (runningTerminals && runningTerminals.length > 0) {
            try {
                const streamId = await streamManagerClient!.getStreamAsync(
                    runningTerminals[0].streamName,
                    runningTerminals[0].streamCondition,
                );
                const channel = await openSshChannel(workspaceClient.sshSession!, streamId);
                channel.onDataReceived((data: any) => {
                    let strData = this.utf8Decoder.decode(data);
                    const processedData = this.processData(strData);
                    this.communicationProvider.writeToTerminalOutput(TerminalId.VMTerminal, processedData);

                });
                channel.onClosed(() => {
                    this.logger.info('Channel closed');
                });
            } catch (e) {
                this.logger.error('Exception on ssh communication');
            }
        }
    }

    private processData(data: string): string {
        // Regex to match the log header with the step declarations
        // Example: ########0-100-InitializeEnvironment-noterminal
        const headerRegex = /#{8}0-\d+-[A-Za-z]\w+-[A-Za-z]\w+/g;
        const match = data.match(headerRegex);
        let steps: {}[] = [];
        if (match) {
            for (const step of match) {
                const [, code, name, terminal] = step.split('-');
                if (!this.stepIdentifiers[name]) {
                    try {
                        // Regex to match the step codes
                        // Example: ########100-Running
                        this.stepIdentifiers[name] = new RegExp(`#{8}${code}-[A-Za-z]\\w+`, 'g');
                        const entry = {
                            name,
                            data: {
                                status: 'Pending',
                                terminal: terminal === 'terminal' ? 'true' : 'false',
                            },
                        };
                        steps.push(entry);
                    } catch (e) {
                        this.logger.error(e);
                    }
                }
            }
            if (steps) {
                this.communicationProvider.appendSteps(steps);
            }
            data = data.replace(headerRegex, '');
        }
        Object.entries(this.stepIdentifiers).forEach(([name, expr]) => {
            const regex = expr as RegExp;
            const match = data.match(regex);
            if (match && match.length > 0) {
                const status = match[match.length - 1].split('-');
                if (status.length > 1) {
                    this.communicationProvider.updateStep({
                        name,
                        status: status[1],
                    });
                    data = data.replace(regex, '');
                }
            }
        });
        return data;
    }
}
