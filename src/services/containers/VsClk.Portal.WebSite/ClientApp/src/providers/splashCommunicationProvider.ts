import { ICommunicationChannel } from '../interfaces/ICommunicationChannel';

export class SplashCommunicationProvider implements ICommunicationChannel {
    constructor(commandCallback:(message: any) => void) {
        window.addEventListener("message", (message) => {
            if (message.origin === window.origin){
                commandCallback(message);
            }
        }, false);
    }
    
    public writeToTerminalOutput(message: string): void {
        window.postMessage({ message }, window.origin);
    }

    private postStep(step: {}) {
        window.postMessage({ step }, window.origin);
    }

    public updateStep(step: {}): void {
        this.postStep(step);
    }

    private postSteps(steps: {}) {
        window.postMessage({ steps }, window.origin);
    }

    public initializeSteps(steps: {}[]) {
        this.postSteps(steps);
    }

    public postEnvironmentId(environmentId: string) {
        window.postMessage({ environmentId });
    }
}