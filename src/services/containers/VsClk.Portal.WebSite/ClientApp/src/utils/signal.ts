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

    constructor() {
        this.promiseToComplete = new Promise((resolve, reject) => {
            this.promiseResolve = resolve;
            this.promiseReject = reject;
        });
    }

    public complete(result: T): void {
        this.promiseResolve(result);
    }

    public completeVoid(this: Signal<void>) {
        this.promiseResolve(undefined);
    }

    public reject(error: Error): void {
        this.promiseReject(error);
    }

    public cancel(): void {
        this.promiseReject(new CancellationError());
    }

    public get promise(): Promise<T> {
        return this.promiseToComplete;
    }
}