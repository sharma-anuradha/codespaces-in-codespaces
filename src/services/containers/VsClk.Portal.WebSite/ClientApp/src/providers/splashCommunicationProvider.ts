import { ICommunicationChannel } from '../interfaces/ICommunicationChannel';
import { ICreationStep, ICreationStepName } from '../interfaces/ICreationStep';

export class SplashCommunicationProvider implements ICommunicationChannel {
    private stepsStatus: ICreationStep[];
    constructor(commandCallback:(message: any) => void) {
        this.stepsStatus = [
            {
                name: 'initializeEnvironment',
                status: 'Pending',
            },
            {
                name: 'buildContainer',
                status: 'Pending',
            },
            {
                name: 'runContainer',
                status: 'Pending',
            },
        ];

        window.addEventListener("message", (message) => {
            if (message.origin === window.origin){
                commandCallback(message);
            }
        }, false);
    }
    
    public writeToTerminalOutput(message: string): void {
        window.postMessage({ message }, window.origin);
    }

    private postStep(step: ICreationStep) {
        window.postMessage({ step }, window.origin);
    }

    public updateStep(step: ICreationStep): void {
        const stepToUpdate = this.stepsStatus.find((entry) => entry.name === step.name);
        if (stepToUpdate) {
            stepToUpdate.status = step.status;
        }
        this.postStep(step);
    }

    public postEnvironmentId(environmentId: string) {
        window.postMessage({ environmentId });
    }
    
    public getStepStatus(step: ICreationStepName): string | undefined {
        const stepToReturn = this.stepsStatus.find((entry) => entry.name === step);
        if (stepToReturn) {
            return stepToReturn.status;
        }
        return undefined;
    }
}