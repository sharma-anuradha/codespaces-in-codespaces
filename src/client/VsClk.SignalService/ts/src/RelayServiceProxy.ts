import * as signalR from '@microsoft/signalr';

import { IRelayServiceProxy, IRelayHubParticipant, IRelayHubProxy, IReceivedData, IParticipantChanged, ParticipantChangeType, SendOption }  from './IRelayServiceProxy';
import { HubProxyBase } from './HubProxyBase';

export class RelayServiceProxy extends HubProxyBase implements IRelayServiceProxy {
    private relayHubs = new Map<string, RelayHubProxy>();

    constructor(
        hubConnection: signalR.HubConnection,
        logger?: signalR.ILogger,
        useSignalRHub?: boolean) {
        super(hubConnection, logger, useSignalRHub ? 'relayServiceHub' : undefined);

        hubConnection.on(this.toHubMethodName('receiveData'), async (hubId, fromParticipantId, uniqueId, type, data) => {
            if (this.logger) {
                this.logger.log(signalR.LogLevel.Debug, `RelayServiceProxy.receiveData hubId:${hubId} fromParticipantId:${fromParticipantId} type:${type}`);
            }

            const relayHub = this.relayHubs.get(hubId);
            if (relayHub) {
                const bufferData = (<any>process).browser ? btoa(data) : Buffer.from(data, 'base64');
                await relayHub._OnReceiveData(fromParticipantId, uniqueId, type, <Uint8Array> bufferData);
            }
        });

        hubConnection.on(this.toHubMethodName('participantChanged'), async (hubId, participantId, properties, changeType) => {
            if (this.logger) {
                this.logger.log(signalR.LogLevel.Debug, `RelayServiceProxy.participantChanged hubId:${hubId} participantId:${participantId} properties:${JSON.stringify(properties)} changeType:${changeType}`);
            }

            const relayHub = this.relayHubs.get(hubId);
            if (relayHub) {
                await relayHub._OnParticipantChanged(participantId, properties, changeType);
            }
        });
    }

    public createHub(hubId?: string): Promise<string> {
        return this.invoke<string>('CreateHubAsync', hubId);
    }

    public async joinHub(hubId: string, properties: { [key: string]: any; }, createIfNotExists: boolean): Promise<IRelayHubProxy> {
        const joinHub = await this.invoke<JoinHubInfo>('JoinHubAsync', hubId, properties, createIfNotExists);
        const realyHubProxy = new RelayHubProxy(this, hubId, joinHub);
        this.relayHubs.set(hubId, realyHubProxy);

        return realyHubProxy;
    }
}

class RelayHubProxy implements IRelayHubProxy {
    private receiveDataCallbacks: Array<(receivedData: IReceivedData) => Promise<void>> = [];
    private participantChangedCallbacks: Array<(participantChanged: IParticipantChanged) => Promise<void>> = [];

    private hubParticipants = new Map<string, IRelayHubParticipant>();

    constructor(
        private readonly relayServiceProxy: RelayServiceProxy,
        public id: string,
        joinHub: JoinHubInfo) {
            joinHub.Participants.forEach(p => {
                const relayHubParticipant: IRelayHubParticipant = {
                    id: p.Id,
                    properties: p.Properties,
                    isSelf: joinHub.ParticipantId === p.Id
                };
                this.hubParticipants.set(p.Id, relayHubParticipant);
            });
    }

    public get participants() {
        return Array.from(this.hubParticipants.values());
    }

    public onReceiveData(callback: (receivedData: IReceivedData) => Promise<void>): void {
        if (callback) {
            this.receiveDataCallbacks.push(callback);
        }
    }

    public onParticipantChanged(callback: (participantChanged: IParticipantChanged) => Promise<void>): void {
        if (callback) {
            this.participantChangedCallbacks.push(callback);
        }
    }

    public sendData(sendOption: SendOption, targetParticipants: string[], type: string, data: Uint8Array): Promise<void> {
        return this.relayServiceProxy.invoke('SendDataHubAsync', sendOption, targetParticipants, type, data );
    }

    public async _OnReceiveData(fromParticipantId: string, uniqueId: number, type: string , data: Uint8Array ): Promise<void> {
        
        const participant = this.hubParticipants.get(fromParticipantId);
        if (participant) {
            const receivedData: IReceivedData = {
                fromParticipant: participant,
                uniqueId,
                type,
                data
            };

            for (const callback of this.receiveDataCallbacks) {
                await callback(receivedData);
            }        
        }
    }

    public async _OnParticipantChanged(participantId: string, properties: { [key: string]: any; }, changeType: ParticipantChangeType): Promise<void> {
        let participant: IRelayHubParticipant | undefined;

        if (changeType === ParticipantChangeType.Added) {
            participant = {
                id: participantId,
                properties,
                isSelf: false
            };

            this.hubParticipants.set(participantId, participant);
        } else if (changeType === ParticipantChangeType.Removed) {
            participant = this.hubParticipants.get(participantId);
            this.hubParticipants.delete(participantId);
        } else if (changeType === ParticipantChangeType.Updated) {
            participant = this.hubParticipants.get(participantId);
            if (participant) {
                (<any>participant).properties = properties;
            }
        }
        
        if (participant) {
            const participantChanged: IParticipantChanged = {
                participant,
                changeType
            };

            for (const callback of this.participantChangedCallbacks) {
                await callback(participantChanged);
            }        
        }
    }
}

interface HubParticipant {
    readonly Id: string;
    readonly Properties: { [key: string]: any; };
}

interface JoinHubInfo {
    readonly ParticipantId: string;
    readonly Participants: HubParticipant[];
}
