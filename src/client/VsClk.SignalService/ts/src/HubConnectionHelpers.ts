import * as signalR from '@aspnet/signalr';
import { ExponentialBackoff } from './ExponentialBackoff';

export async function connect(
    hubConnection: signalR.HubConnection,
    onConnectCallback: (retries: number, backoffTime?: number, error?: Error) => Promise<number>,
    maxRetries: number,
    delayMilliseconds: number,
    maxDelayMilliseconds: number,
    logger: signalR.ILogger) {
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
            logger.log(signalR.LogLevel.Error, `Failed to connect-> delay:${delay} err:${error}`);
            delay = await onConnectCallback(exponentialBackoff.retriesCount, delay, error);
            if (delay === -1) {
                break;
            }
            
            await sleep(delay);
        }
    }
}

function sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(() => resolve(), ms));
}
  