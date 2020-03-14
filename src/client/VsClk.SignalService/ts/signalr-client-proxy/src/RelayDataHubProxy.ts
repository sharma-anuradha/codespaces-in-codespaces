import { IRelayDataHubProxy, IReceivedData } from './IRelayServiceProxy';
import { CallbackContainer } from './CallbackContainer';
import { IDisposable } from './IDisposable';

export class RelayDataHubProxy implements IRelayDataHubProxy, IDisposable {
    private receiveDataCallbacks = new CallbackContainer<(receivedData: IReceivedData) => Promise<void>>();
    private readonly eventDisposable: IDisposable;
    constructor(
        source: IRelayDataHubProxy,
        filterEventCallback: (receivedData: IReceivedData) => boolean) {
        this.eventDisposable = source.onReceiveData((e) => {
            if (!filterEventCallback(e)) {
                return Promise.resolve();
            }

            return this.processReceivedData(e);
        });
    }

    public dispose() {
        this.eventDisposable.dispose();
    }

    public onReceiveData(callback: (receivedData: IReceivedData) => Promise<void>): IDisposable {
        return this.receiveDataCallbacks.add(callback);
    }

    protected processReceivedData(receivedData: IReceivedData): Promise<void> {
        return this.fireReceivedData(receivedData);
    }

    protected async fireReceivedData(receivedData: IReceivedData): Promise<void> {
        for (const callback of this.receiveDataCallbacks.items) {
            await callback(receivedData);
        }        
    }
}