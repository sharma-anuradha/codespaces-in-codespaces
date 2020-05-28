import { TerminalId } from './TerminalId';

export interface ICommunicationChannel {
    writeToTerminalOutput(id: TerminalId, message: string): void;
    updateStep(step: {}): void;
    sendNotification(message: string): void;
}
