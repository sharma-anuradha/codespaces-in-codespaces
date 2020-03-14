
import { IRelayHubProxy, SendOption, ParticipantChangeType, SequenceRelayDataHubProxy, RelayHubMessageProperties, IDisposable } from '@vs/vso-signalr-client-proxy';
import { IDataChannel, ChannelClosedEventArgs } from './IDataChannel';
import { Event, Emitter, CancellationToken } from 'vscode-jsonrpc';

export class RelayDataChannel implements IDataChannel, IDisposable {
    private readonly dataReceivedEmitter = new Emitter<Buffer>();
    private nextSequence = 0;
    private disposables: IDisposable[] = [];

    public readonly onDataReceived: Event<Buffer> = this.dataReceivedEmitter.event;

    private readonly closedEmitter = new Emitter<ChannelClosedEventArgs>();
    public readonly onClosed: Event<ChannelClosedEventArgs> = this.closedEmitter.event;

    constructor(
        private readonly relayHubProxy: IRelayHubProxy,
        private readonly streamId: string,
        private readonly targetParticipant: string) {
        
        const sequenceRelayDataHubProxy = new SequenceRelayDataHubProxy(
            relayHubProxy,
            (e) => e.type === this.streamId && e.fromParticipant.id === this.targetParticipant);
        this.disposables.push(sequenceRelayDataHubProxy);
        this.disposables.push(sequenceRelayDataHubProxy.onReceiveData((e) => {

            this.dataReceivedEmitter.fire(Buffer.from(e.data));

            return Promise.resolve();
        }));

        this.disposables.push(relayHubProxy.onParticipantChanged((e) => {
            if (e.changeType === ParticipantChangeType.Removed && e.participant.id === this.targetParticipant) {
                this.fireClosed({
                    error: new Error(`participant id:${e.participant.id} removed`)
                });
            }
            
            return Promise.resolve();
        }));

        this.disposables.push(relayHubProxy.onDisconnected(async () => {
            this.fireClosed({
                error: new Error(`hub proxy disconnected`)
            });

            return Promise.resolve();
        }));
    }

    public dispose() {
        this.disposables.forEach((d) => d.dispose());
        this.disposables = [];
    }

    public async send(data: Buffer, cancellation?: CancellationToken): Promise<void> {
        await this.relayHubProxy.sendData(
            SendOption.ExcludeSelf,
            [ this.targetParticipant ],
            this.streamId,
            data,
            RelayHubMessageProperties.createMessageSequence(++this.nextSequence));
    }

    private fireClosed(e: ChannelClosedEventArgs) {
        this.closedEmitter.fire(e);
    }
}