import { ICreationStep } from './ICreationStep';

export interface ICommunicationChannel {
    writeToTerminalOutput(message: string): void;
    updateStep(step: ICreationStep): void;
}
