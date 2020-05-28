import { IConnectionAdapter } from '@vs/vso-splash-screen';

export class ConnectionAdapter implements IConnectionAdapter {
    sendCommand(command: string, args: {
        environmentId: string;
    }) {
        switch (command) {
            case 'connect':
                window.postMessage({
                    command,
                    environmentId: args.environmentId,
                }, window.origin);
                break;
        }
    }
    onMessage(callback: (ev: MessageEvent) => any) {
        window.addEventListener('message', callback, false);
    }
}
