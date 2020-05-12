export class CriticalError extends Error {
    constructor(message: string) {
        super(message);

        if (typeof Error.captureStackTrace === 'function') {
            Error.captureStackTrace(this, CriticalError);
        }
    }
}
