import { isThenable } from './isThenable';
import { Disposable, CancellationToken } from 'vscode-jsonrpc';

export class CancellationError extends Error {
    constructor(message?: string, code?: string) {
        super(message);
        this.code = code;
    }

    public code: any;
}

export class Signal<T> implements Disposable {
    private promiseToComplete: Promise<T>;
    private promiseResolve!: (result: T) => void;
    private promiseReject!: (error: any) => void;

    private _isFulfilled = false;
    private _isResolved = false;
    private _isRejected = false;

    public get isFulfilled() {
        return this._isFulfilled;
    }
    public get isResolved() {
        return this._isResolved;
    }
    public get isRejected() {
        return this._isRejected;
    }

    constructor(cancellationToken?: CancellationToken) {
        // tslint:disable-next-line: promise-must-complete
        this.promiseToComplete = new Promise((resolve, reject) => {
            this.promiseResolve = resolve;
            this.promiseReject = reject;
        });

        this.complete = this.complete.bind(this);
        this.reject = this.reject.bind(this);
        this.cancel = this.cancel.bind(this);

        if (!cancellationToken) {
            return;
        }

        if (cancellationToken.isCancellationRequested) {
            this.cancel();
        } else {
            cancellationToken.onCancellationRequested(() => {
                if (this.isFulfilled) {
                    return;
                }

                this.cancel();
            });
        }
    }

    public complete(result: T): void {
        this._isFulfilled = true;
        this._isResolved = true;
        this._isRejected = false;

        this.promiseResolve(result);
    }

    public reject(error: Error): void {
        this._isFulfilled = true;
        this._isResolved = false;
        this._isRejected = true;

        this.promiseReject(error);
    }

    public cancel(): void {
        this._isFulfilled = true;
        this._isResolved = false;
        this._isRejected = true;

        this.promiseReject(new CancellationError());
    }

    public get promise(): Promise<T> {
        return this.promiseToComplete;
    }

    public static from<T>(value: Promise<T>): Signal<T>;
    public static from<T>(value: T): Signal<T>;
    public static from<T>(value: T | Promise<T>): Signal<T> {
        const signal = new Signal<T>();

        if (isThenable(value)) {
            value.then(signal.complete, signal.reject);
        } else {
            signal.complete(value);
        }

        return signal;
    }

    dispose(): void {
        if (this.isFulfilled) {
            return;
        }

        this.cancel();
    }
}
