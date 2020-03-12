export interface ICommunicationChannel {
    writeToTerminalOutput(message: string): void;
    updateStep(step: {}): void;
}
