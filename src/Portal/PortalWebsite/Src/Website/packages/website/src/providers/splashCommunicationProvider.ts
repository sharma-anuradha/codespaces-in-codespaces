import { ICommunicationChannel } from '../interfaces/ICommunicationChannel';
import { TerminalId } from '../constants';

export class SplashCommunicationProvider implements ICommunicationChannel {
    constructor(commandCallback: (message: any) => void) {
        window.addEventListener(
            'message',
            (message) => {
                if (message.origin === window.origin) {
                    commandCallback(message);
                }
            },
            false
        );
    }

    public writeToTerminalOutput(id: TerminalId, message: string): void {
        window.postMessage(
            {
                terminal: {
                    id,
                    message,
                },
            },
            window.origin
        );
    }

    private postStep(step: {}) {
        window.postMessage({ step }, window.origin);
    }

    public updateStep(step: {}): void {
        this.postStep(step);
    }

    public postEnvironmentId(environmentId: string) {
        window.postMessage({ environmentId }, window.origin);
    }

    public sendNotification(message: string): void {
        window.postMessage(
            {
                notification: {
                    message,
                },
            },
            window.origin
        );
    }

    public appendSteps(steps: {}[]) {
        window.postMessage({ appendSteps: steps }, window.origin);
    }
}
