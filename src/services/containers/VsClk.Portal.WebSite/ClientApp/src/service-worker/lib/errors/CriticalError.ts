export class CriticalError extends Error {
    constructor(message: string) {
        super(message);
        Error.captureStackTrace(this, CriticalError);
    }
}
