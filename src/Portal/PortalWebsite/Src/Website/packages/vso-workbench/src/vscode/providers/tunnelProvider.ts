import { ITunnelProvider, ITunnel, ITunnelOptions } from 'vscode-web';
import { Emitter } from 'vscode-jsonrpc';

const onDidDispose = new Emitter<void>();
// const onDidDispose: changeEventEmitter.EventEmitter<void> = new changeEventEmitter.EventEmitter();

// Note: ports that need to be filtered from PF UI, changing this list requires changing in Cascade as well.
const serverProcessesFilter = ['vsls-agent', 'vscode-remote'];

export class TunnelProvider implements ITunnelProvider {
    constructor(
        private readonly ensureConnection: (tunnelOptions: ITunnelOptions) => Promise<void>
    ) {}

    tunnelFactory = async (tunnelOptions: ITunnelOptions): Promise<ITunnel> => {
        const port = tunnelOptions.localAddressPort
            ? tunnelOptions.localAddressPort
            : tunnelOptions.remoteAddress.port;

        await this.ensureConnection(tunnelOptions);

        return {
            remoteAddress: tunnelOptions.remoteAddress,
            localAddress: '127.0.0.1:' + port,
            onDidDispose: onDidDispose.event,
            dispose: () => {
                onDidDispose.fire();
            },
        };
    };

    showPortCandidate = (host: string, port: number, detail: string): Promise<boolean> => {
        return Promise.resolve(!serverProcessesFilter.some((filter) => detail.includes(filter)));
    };
}
