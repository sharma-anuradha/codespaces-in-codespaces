import { RelayDataHubProxy } from './RelayDataHubProxy';
import { IRelayDataHubProxy, IReceivedData } from './IRelayServiceProxy';
import { RelayHubMessageProperties } from './RelayHubMessageProperties';

export class SequenceRelayDataHubProxy extends RelayDataHubProxy {
    private readonly receiveDataBuffer = new Map<number, IReceivedData> ();
    constructor(
        source: IRelayDataHubProxy,
        filterEventCallback: (receivedData: IReceivedData) => boolean,
        currentSequence?: number) {
        super(source, filterEventCallback);
        if (currentSequence) {
            this.currentSequence = currentSequence;
        } else {
            this.currentSequence = 0;
        }

        this.totalEvents = 0;
    }

    public currentSequence: number;

    public totalEvents: number;

    protected async processReceivedData(receivedData: IReceivedData): Promise<void> {
        const sequence = RelayHubMessageProperties.getMessageSequence(receivedData.properties);
        if (!sequence || sequence === -1) {
            await this.fireReceivedData(receivedData);
            return;
        }

        if (this.currentSequence === -1 || (this.currentSequence + 1) === sequence) {
            this.currentSequence = sequence;
            await this.fireReceivedData(receivedData);
            while (true) {
                const nextReceivedData = this.tryRemove(this.currentSequence + 1);
                if (nextReceivedData) {
                    await this.fireReceivedData(nextReceivedData);
                    ++this.currentSequence;
                } else {
                    break;
                }
            }
        } else {
            if (sequence > this.currentSequence && (sequence - this.currentSequence) > 10) {
                console.debug(`###-----> sequence/current:${sequence}/${this.currentSequence}`);
            }

            this.receiveDataBuffer.set(sequence, receivedData);
            ++this.totalEvents;
        }
    }

    private tryRemove(sequence: number): IReceivedData | undefined {
        const receivedData = this.receiveDataBuffer.get(sequence);
        if (receivedData) {
            this.receiveDataBuffer.delete(sequence);
            return receivedData;
        }
    }
}