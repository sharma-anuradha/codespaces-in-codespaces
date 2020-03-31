import { TerminalId } from '../constants';

export interface ICommunicationChannel {
    writeToTerminalOutput(id: TerminalId, message: string): void;
    updateStep(step: {}): void;
}
