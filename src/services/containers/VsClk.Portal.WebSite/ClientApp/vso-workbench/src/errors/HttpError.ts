export class HttpError extends Error {
    public errorType = 'HttpError';

    constructor(public statusCode: number, public statusText: string) {
        super(`${statusCode} ${statusText}`);
    }
}
