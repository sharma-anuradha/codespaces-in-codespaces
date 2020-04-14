import { BaseStream }  from '@vs/vs-ssh';
import { CancellationToken }  from 'vscode-jsonrpc';
import { IRelayHubProxy, SendOption, ParticipantChangeType, SequenceRelayDataHubProxy, RelayHubMessageProperties, IDisposable } from '@vs/vso-signalr-client-proxy';

export class HubStream extends BaseStream {
    private nextSequence = 0;
    private disposables: IDisposable[] = [];

    constructor(
        private readonly relayHubProxy: IRelayHubProxy,
        private readonly streamId: string,
        private readonly targetParticipant: string) {
        super();
        const sequenceRelayDataHubProxy = new SequenceRelayDataHubProxy(
            relayHubProxy,
            (e) => e.type === this.streamId && e.fromParticipant.id === this.targetParticipant);

        this.disposables.push(sequenceRelayDataHubProxy);
        this.disposables.push(sequenceRelayDataHubProxy.onReceiveData((e) => {
            this.onData(Buffer.from(e.data));
            return Promise.resolve();
        }));

        this.disposables.push(relayHubProxy.onParticipantChanged((e) => {
            if (e.changeType === ParticipantChangeType.Removed && e.participant.id === this.targetParticipant) {
                this.close(new Error(`participant id:${e.participant.id} removed`));
            }
            
            return Promise.resolve();
        }));
        
        const hubProxy = relayHubProxy.relayServiceProxy.hubProxy;
        this.disposables.push(hubProxy.onConnectionStateChanged(() => {
            if (!hubProxy.isConnected) {
                this.close(new Error(`hub proxy was disconnected`));
            }

            return Promise.resolve();
        }));
    }

    public async write(data: Buffer, cancellation?: CancellationToken): Promise<void> {
        if (!data) throw new TypeError('Data is required.');
		if (this.disposed) throw new Error('HubStream diposed');

        await this.relayHubProxy.sendData(
            SendOption.ExcludeSelf,
            [ this.targetParticipant ],
            this.streamId,
            data,
            RelayHubMessageProperties.createMessageSequence(++this.nextSequence));
    }

    public close(error?: Error, cancellation?: CancellationToken): Promise<void> {
        this.disposables.forEach((d) => d.dispose());
        this.disposables = [];

        this.onError(error || new Error('Stream closed.'));
        this.closedEmitter.fire({ error });
        
        return Promise.resolve();
    }
}