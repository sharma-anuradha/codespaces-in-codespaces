import { Disposable, CancellationToken } from 'vscode-jsonrpc';

import { createCancellationToken, Signal } from 'vso-client-core';

export class RequestStore<TResponse> implements Disposable {
    private trackedRequests = new Map<string, Signal<TResponse>>();
    private disposables: Disposable[] = [];

    constructor(private readonly options = { defaultTimeout: 30 * 1000 }) {}

    setResponse(key: string, response: TResponse): void {
        if (!this.trackedRequests.has(key)) {
            this.trackedRequests.set(key, Signal.from(response));
        } else {
            const signal = this.trackedRequests.get(key)!;
            signal.complete(response);
        }
    }

    getResponse(key: string, cancellationToken?: CancellationToken): Promise<TResponse> {
        if (!cancellationToken) {
            const tokenSource = createCancellationToken(this.options.defaultTimeout);
            this.disposables.push(tokenSource);
            cancellationToken = tokenSource.token;
        }
        if (!this.trackedRequests.has(key)) {
            const signal = new Signal<TResponse>(cancellationToken);
            this.trackedRequests.set(key, signal);
            return signal.promise;
        } else {
            return this.trackedRequests.get(key)!.promise;
        }
    }

    dispose(): void {
        for (const response of this.trackedRequests.values()) {
            response.dispose();
        }
        this.trackedRequests.clear();
        for (const disposable of this.disposables) {
            disposable.dispose();
        }
        this.disposables = [];
    }
}
