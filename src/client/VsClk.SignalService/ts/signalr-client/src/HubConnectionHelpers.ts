import * as signalR from '@microsoft/signalr';
import { IDisposable } from '@vs/vso-signalr-client-proxy';
import { ExponentialBackoff } from './ExponentialBackoff';
import { CancellationToken } from './CancellationToken';

export async function connect(
    hubConnection: signalR.HubConnection,
    onConnectCallback: (retries: number, backoffTime?: number, error?: Error) => Promise<number>,
    maxRetries: number,
    delayMilliseconds: number,
    maxDelayMilliseconds: number,
    logger: signalR.ILogger,
    cancellationToken: CancellationToken) {
    const exponentialBackoff = new ExponentialBackoff(maxRetries, delayMilliseconds, maxDelayMilliseconds);
    while (true) {
        try {
            logger.log(signalR.LogLevel.Debug, `hubConnection.start -> retries:${exponentialBackoff.retriesCount}`);
            await hubConnection.start();
            await onConnectCallback(exponentialBackoff.retriesCount);
            logger.log(signalR.LogLevel.Debug, 'Succesfully connected...');
            break;
        } catch (error) {
            let delay = exponentialBackoff.nextDelayMilliseconds();
            logger.log(signalR.LogLevel.Error, `Failed to connect-> delay:${delay} err:${JSON.stringify(error)}`);
            delay = await onConnectCallback(exponentialBackoff.retriesCount, delay, error);
            if (delay === -1) {
                break;
            }
            
            await sleep(delay, cancellationToken);
        }
    }
}

function sleep(ms: number, cancellationToken: CancellationToken): Promise<void> {
    return new Promise(resolve => {
        let disposable: IDisposable;

        const resolveCallback = () => {
            if (disposable) {
                disposable.dispose();
            }
            resolve();
        };
        const timeoutId = setTimeout(() => {
            resolveCallback();
        }, ms);
        disposable = cancellationToken.onCancellationRequest(() => {
            clearTimeout(timeoutId);
            resolveCallback();
        });
    });
}
  