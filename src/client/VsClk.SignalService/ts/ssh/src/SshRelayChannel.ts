
import { IRelayHubProxy, SendOption, ParticipantChangeType, RelayDataHubProxy, SequenceRelayDataHubProxy, RelayHubMessageProperties, IDisposable } from '@vs/vso-signalr-client-proxy';
import { Event, Emitter, CancellationToken } from 'vscode-jsonrpc';
import { SshChannel, SshRpcMessageStream }  from '@vs/vs-ssh';

export function createSshRpcMessageStream(
    relayHubProxy: IRelayHubProxy,
    streamId: string,
    targetParticipant: string,
    closeOnError?: boolean,
    relayDataHubProxy?: RelayDataHubProxy): SshRpcMessageStream  {

    const sshChannel = <SshChannel>(<any>new SshRelayChannel(
        relayHubProxy,
        streamId,
        targetParticipant,
        relayDataHubProxy || SequenceRelayDataHubProxy.createForTypeAndParticipant(relayHubProxy, streamId, targetParticipant),
        closeOnError));
    return new SshRpcMessageStream(sshChannel);
}

class ChannelClosedEventArgs {
    public readonly errorMessage?: string;
    public readonly error?: Error;
}

class SshRelayChannel implements IDisposable {
    private readonly dataReceivedEmitter = new Emitter<Buffer>();
    private nextSequence = 0;
    private disposables: IDisposable[] = [];

    public readonly onDataReceived: Event<Buffer> = this.dataReceivedEmitter.event;

    private readonly closedEmitter = new Emitter<ChannelClosedEventArgs>();
    public readonly onClosed: Event<ChannelClosedEventArgs> = this.closedEmitter.event;

    constructor(
        private readonly relayHubProxy: IRelayHubProxy,
        private readonly streamId: string,
        private readonly targetParticipant: string,
        relayDataHubProxy: RelayDataHubProxy,
        private readonly closeOnError?: boolean) {
        
        this.disposables.push(relayDataHubProxy);
        this.disposables.push(relayDataHubProxy.onReceiveData((e) => {

            this.dataReceivedEmitter.fire(Buffer.from(e.data));

            return Promise.resolve();
        }));

        this.disposables.push(relayHubProxy.onParticipantChanged((e) => {
            if (e.changeType === ParticipantChangeType.Removed && e.participant.id === this.targetParticipant) {
                this.fireError(new Error(`participant id:${e.participant.id} removed`));
            }
            
            return Promise.resolve();
        }));

        this.disposables.push(relayHubProxy.onDisconnected(async () => {
            this.fireError(new Error(`hub proxy disconnected`));

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

    public adjustWindow(messageLength: number): void {
        // called by the SshRpcMessageReader class when data is received
    }

    private fireError(error: Error) {
        // fire the error
        this.closedEmitter.fire({
            error
        });
        // enforce the rpc message reader/writer to close
        if (this.closeOnError) {
            this.closedEmitter.fire({});
        }
    }
}