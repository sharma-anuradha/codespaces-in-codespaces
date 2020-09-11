import { ITunnelProvider, ITunnel, ITunnelOptions } from 'vscode-web';
import { Emitter, Event } from 'vscode-jsonrpc';

const onDidDispose = new Emitter<void>();
// const onDidDispose: changeEventEmitter.EventEmitter<void> = new changeEventEmitter.EventEmitter();

// Note: ports that need to be filtered from PF UI, changing this list requiers changing in Cascade as well.
const serverProcessesFilter = ['vsls-agent', 'vscode-remote'];

export class TunnelProvider implements ITunnelProvider {
    constructor() {
        this.tunnelFactory = this.tunnelFactory;
        this.showPortCandidate = this.showPortCandidate;
    }

    tunnelFactory = (tunnelOptions: ITunnelOptions): Promise<ITunnel> | undefined => {
        const port = tunnelOptions.localAddressPort
            ? tunnelOptions.localAddressPort
            : tunnelOptions.remoteAddress.port;
        return Promise.resolve({
            remoteAddress: tunnelOptions.remoteAddress,
            localAddress: '127.0.0.1:' + port, 
            onDidDispose: onDidDispose.event,
            dispose: () => {
                onDidDispose.fire();
            },
        });
    };

    showPortCandidate = (host: string, port: number, detail: string): Promise<boolean> => {
        return Promise.resolve(!serverProcessesFilter.some((filter) => detail.includes(filter)));
    };
}
