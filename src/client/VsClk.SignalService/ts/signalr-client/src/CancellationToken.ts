import { CallbackContainer, IDisposable } from '@vs/vso-signalr-client-proxy';

export class CancellationToken {
    private cancellationRequestCallbacks = new CallbackContainer<() => void>();

    public onCancellationRequest(callback: () => void): IDisposable {
        return this.cancellationRequestCallbacks.add(callback);
    }

    public cancel(): void {
        for (const callback of this.cancellationRequestCallbacks.items) {
            callback();
        }   
    }
}
