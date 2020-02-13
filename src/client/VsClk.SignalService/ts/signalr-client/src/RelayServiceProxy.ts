
import { IRelayServiceProxy, IRelayHubParticipant, IRelayHubProxy, IReceivedData, IParticipantChanged, ParticipantChangeType, SendOption }  from './IRelayServiceProxy';
import { HubProxyBase } from './HubProxyBase';
import { IHubProxy } from './IHubProxy';
import { LogLevel } from '@microsoft/signalr';

export class RelayServiceProxy extends HubProxyBase implements IRelayServiceProxy {
    private relayHubs = new Map<string, RelayHubProxy>();

    constructor(
        hubProxy: IHubProxy,
        logger?: signalR.ILogger,
        useSignalRHub?: boolean) {
        super(hubProxy, logger, useSignalRHub ? 'relayServiceHub' : undefined);

        hubProxy.on(this.toHubMethodName('receiveData'), async (hubId, fromParticipantId, uniqueId, type, data) => {
            if (this.logger) {
                this.logger.log(LogLevel.Debug, `RelayServiceProxy.receiveData hubId:${hubId} fromParticipantId:${fromParticipantId} type:${type}`);
            }

            const relayHub = this.relayHubs.get(hubId);
            if (relayHub) {
                const bufferData = (<any>process).browser ? btoa(data) : Buffer.from(data, 'base64');
                await relayHub._OnReceiveData(fromParticipantId, uniqueId, type, <Uint8Array> bufferData);
            }
        });

        hubProxy.on(this.toHubMethodName('participantChanged'), async (hubId, participantId, properties, changeType) => {
            if (this.logger) {
                this.logger.log(LogLevel.Debug, `RelayServiceProxy.participantChanged hubId:${hubId} participantId:${participantId} properties:${JSON.stringify(properties)} changeType:${changeType}`);
            }

            const relayHub = this.relayHubs.get(hubId);
            if (relayHub) {
                await relayHub._OnParticipantChanged(participantId, properties, changeType);
            }
        });

        hubProxy.on(this.toHubMethodName('hubDeleted'), async (hubId) => {
            if (this.logger) {
                this.logger.log(LogLevel.Debug, `RelayServiceProxy.hubDeleted hubId:${hubId}`);
            }

            const relayHub = this.relayHubs.get(hubId);
            if (relayHub) {
                this.relayHubs.delete(hubId);
                await relayHub._OnDeleted();
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

    public async deleteHub(hubId: string): Promise<void> {
        await this.invoke('DeleteHubAsync', hubId);
    }
}

class RelayHubProxy implements IRelayHubProxy {
    private receiveDataCallbacks: Array<(receivedData: IReceivedData) => Promise<void>> = [];
    private participantChangedCallbacks: Array<(participantChanged: IParticipantChanged) => Promise<void>> = [];
    private hubDeletedCallbacks: Array<() => Promise<void>> = [];

    private hubParticipants = new Map<string, IRelayHubParticipant>();

    constructor(
        public readonly relayServiceProxy: RelayServiceProxy,
        public id: string,
        private readonly joinHub: JoinHubInfo) {
            joinHub.participants.forEach(p => {
                const relayHubParticipant: IRelayHubParticipant = {
                    id: p.id,
                    properties: p.properties,
                    isSelf: joinHub.participantId === p.id
                };
                this.hubParticipants.set(p.id, relayHubParticipant);
            });
    }

    public get serviceId(): string {
        return this.joinHub.serviceId;
    }

    public get stamp(): string {
        return this.joinHub.stamp;
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

    public onDeleted(callback: () => Promise<void>): void {
        if (callback) {
            this.hubDeletedCallbacks.push(callback);
        }
    }

    public sendData(sendOption: SendOption, targetParticipants: string[] | null, type: string, data: Uint8Array): Promise<void> {
        const dataArray = Array.from(data);
        return this.relayServiceProxy.invoke('SendDataHubAsync', this.id, sendOption, targetParticipants, type, dataArray );
    }

    public async _OnDeleted(): Promise<void> {      
        for (const callback of this.hubDeletedCallbacks) {
            await callback();
        }        
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
    readonly id: string;
    readonly properties: { [key: string]: any; };
}

interface JoinHubInfo {
    readonly serviceId: string;
    readonly stamp: string;
    readonly participantId: string;
    readonly participants: HubParticipant[];
}
