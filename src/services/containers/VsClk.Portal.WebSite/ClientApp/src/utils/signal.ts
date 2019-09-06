//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

export class CancellationError extends Error {
    constructor(message?: string, code?: string) {
        super(message);
        this.code = code;
    }

    public code: any;
}

export class Signal<T> {
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

    constructor() {
        // tslint:disable-next-line: promise-must-complete
        this.promiseToComplete = new Promise((resolve, reject) => {
            this.promiseResolve = resolve;
            this.promiseReject = reject;
        });
    }

    public complete(result: T): void {
        this._isFulfilled = true;
        this._isResolved = true;
        this._isRejected = false;

        this.promiseResolve(result);
    }

    public completeVoid(this: Signal<void>) {
        this._isFulfilled = true;
        this._isResolved = true;
        this._isRejected = false;

        this.promiseResolve(undefined);
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
}
