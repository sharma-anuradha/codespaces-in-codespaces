
import { IRelayHubProxy, SendOption, ParticipantChangeType } from '@vs/vso-signalr-client-proxy';
import { IDataChannel, ChannelClosedEventArgs } from './IDataChannel';
import { Event, Emitter, CancellationToken } from 'vscode-jsonrpc';

export interface IRelayDataProvider {
    encode(data: Buffer): Buffer;
    decode(data: Buffer): Buffer;
}

export class RelayDataChannel implements IDataChannel {
    private readonly dataReceivedEmitter = new Emitter<Buffer>();
    private relayDataProvider: IRelayDataProvider;

    public readonly onDataReceived: Event<Buffer> = this.dataReceivedEmitter.event;

    private readonly closedEmitter = new Emitter<ChannelClosedEventArgs>();
    public readonly onClosed: Event<ChannelClosedEventArgs> = this.closedEmitter.event;

    constructor(
        private readonly relayHubProxy: IRelayHubProxy,
        private readonly streamId: string,
        private readonly targetParticipant: string,
        relayDataProvider?: IRelayDataProvider) {

            this.relayDataProvider = relayDataProvider || {
                encode: (data) => data,
                decode: (data) => data
            };
            relayHubProxy.onReceiveData((e) => {
                if (e.type === this.streamId && e.fromParticipant.id === this.targetParticipant) {
                    this.dataReceivedEmitter.fire(this.relayDataProvider.decode(Buffer.from(e.data)));
                }

                return Promise.resolve();
            });

            relayHubProxy.onParticipantChanged((e) => {
                if (e.changeType === ParticipantChangeType.Removed && e.participant.id === this.targetParticipant) {
                    this.fireClosed({
                        error: new Error(`participant id:${e.participant.id} removed`)
                    });
                }
                
                return Promise.resolve();
            });

            relayHubProxy.onDisconnected(async () => {
                this.fireClosed({
                    error: new Error(`hub proxy disconnected`)
                });

                return Promise.resolve();
            });
    }

    public send(data: Buffer, cancellation?: CancellationToken): Promise<void> {
        return this.relayHubProxy.sendData(
            SendOption.ExcludeSelf,
            [ this.targetParticipant ],
            this.streamId,
            this.relayDataProvider.encode(data));
    }

    private fireClosed(e: ChannelClosedEventArgs) {
        this.closedEmitter.fire(e);
    }
}