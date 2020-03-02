
/**
 * Note: this enum is a replica of the signalR declaration to remove dependency on the proxy lib
 */
export enum LogLevel {
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6
}

export interface ILogger {
    log(logLevel: LogLevel, message: string): void;
}