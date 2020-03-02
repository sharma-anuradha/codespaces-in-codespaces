
import { IRelayServiceProxy, IRelayHubParticipant, IRelayHubProxy, IReceivedData, IParticipantChanged, ParticipantChangeType, SendOption, JoinOptions, SendHubData }  from './IRelayServiceProxy';
import { HubProxyBase, keysToPascal } from './HubProxyBase';
import { IHubProxy } from './IHubProxy';
import { CallbackContainer } from './CallbackContainer';
import { IDisposable } from './IDisposable';
import { ILogger, LogLevel } from './ILogger';

export class RelayServiceProxy extends HubProxyBase implements IRelayServiceProxy {
    private relayHubs = new Map<string, RelayHubProxy>();

    constructor(
        hubProxy: IHubProxy,
        logger?: ILogger,
        useSignalRHub?: boolean) {
        super(hubProxy, logger, useSignalRHub ? 'relayServiceHub' : undefined);

        const isNode = (typeof process !== 'undefined') && (typeof process.release !== 'undefined') && (process.release.name === 'node');

        hubProxy.onConnectionStateChanged(async () => {
            if (!hubProxy.isConnected) {
                await Promise.all(Array.from(this.relayHubs.values()).map(r => r._OnDisconnected()));
                this.relayHubs.clear();
            }
        });

        hubProxy.on(this.toHubMethodName('receiveData'), async (hubId, fromParticipantId, uniqueId, type, data) => {
            if (this.logger) {
                this.logger.log(LogLevel.Debug, `receiveData -> hubId:${hubId} fromParticipantId:${fromParticipantId} type:${type} length:${data.length}`);
            }

            const relayHub = this.relayHubs.get(hubId);
            if (relayHub) {
                let bufferData: any;
                if (typeof data === 'string') {
                    bufferData = isNode ? Buffer.from(data, 'base64') : atob(data);
                } else {
                    bufferData = data;
                }
                await relayHub._OnReceiveData(fromParticipantId, uniqueId, type, <Uint8Array> bufferData);
            }
        });

        hubProxy.on(this.toHubMethodName('participantChanged'), async (hubId, participantId, properties, changeType) => {
            if (this.logger) {
                this.logger.log(LogLevel.Debug, `participantChanged -> hubId:${hubId} participantId:${participantId} properties:${JSON.stringify(properties)} changeType:${changeType}`);
            }

            const relayHub = this.relayHubs.get(hubId);
            if (relayHub) {
                await relayHub._OnParticipantChanged(participantId, properties, changeType);
            }
        });

        hubProxy.on(this.toHubMethodName('hubDeleted'), async (hubId) => {
            if (this.logger) {
                this.logger.log(LogLevel.Debug, `hubDeleted -> hubId:${hubId}`);
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

    public joinHub(hubId: string, properties: { [key: string]: any; }, joinOptions: JoinOptions): Promise<IRelayHubProxy> {
        return this._joinHubInternal(
            (joinHubInfo) => new RelayHubProxy(this, hubId, joinHubInfo),
            hubId,
            properties,
            joinOptions);
    }

    public async deleteHub(hubId: string): Promise<void> {
        await this.invoke('DeleteHubAsync', hubId);
    }

    public async _joinHubInternal( relayHubProxyFactory: (joinHubInfo: JoinHubInfo) => RelayHubProxy, hubId: string, properties: { [key: string]: any; }, joinOptions: JoinOptions): Promise<IRelayHubProxy> {
        const joinHubInfo = await this.invokeKeysToCamel<JoinHubInfo>('JoinHubAsync', hubId, properties, keysToPascal(joinOptions));
        const realyHubProxy = relayHubProxyFactory(joinHubInfo);
        this.relayHubs.set(hubId, realyHubProxy);

        return realyHubProxy;
    }
}

class RelayHubProxy implements IRelayHubProxy {
    private receiveDataCallbacks = new CallbackContainer<(receivedData: IReceivedData) => Promise<void>>();
    private participantChangedCallbacks = new CallbackContainer<(participantChanged: IParticipantChanged) => Promise<void>>();
    private hubDeletedCallbacks = new CallbackContainer<() => Promise<void>>();
    private hubDisconnectedCallbacks = new CallbackContainer<() => Promise<void>>();
    private joinHubInfo: JoinHubInfo | null = null;
    private selfParticipantInstance: IRelayHubParticipant | null = null;
    private isDisconected: boolean = false;
    private hubParticipants = new Map<string, IRelayHubParticipant>();

    constructor(
        public readonly relayServiceProxy: RelayServiceProxy,
        public id: string,
        joinHubInfo: JoinHubInfo) {
        this.setJoinHubInfo(joinHubInfo);
    }

    private setJoinHubInfo(joinHubInfo: JoinHubInfo) {
        this.hubParticipants.clear();

        joinHubInfo.participants.forEach(p => {
            const relayHubParticipant: IRelayHubParticipant = {
                id: p.id,
                properties: p.properties,
            };

            if (joinHubInfo.participantId === p.id) {
                this.selfParticipantInstance = relayHubParticipant;
            }

            this.hubParticipants.set(p.id, relayHubParticipant);
        });

        this.joinHubInfo = joinHubInfo;
    }

    public get serviceId(): string {
        return this.joinHubInfo!.serviceId;
    }

    public get stamp(): string {
        return this.joinHubInfo!.stamp;
    }

    public get selfParticipant() {
        return this.selfParticipantInstance!;
    }

    public get participants() {
        return Array.from(this.hubParticipants.values());
    }

    public onReceiveData(callback: (receivedData: IReceivedData) => Promise<void>): IDisposable {
        return this.receiveDataCallbacks.add(callback);
    }

    public onParticipantChanged(callback: (participantChanged: IParticipantChanged) => Promise<void>): IDisposable {
        return this.participantChangedCallbacks.add(callback);
    }

    public onDeleted(callback: () => Promise<void>): IDisposable {
        return this.hubDeletedCallbacks.add(callback);
    }

    public onDisconnected(callback: () => Promise<void>): IDisposable {
        return this.hubDisconnectedCallbacks.add(callback);
    }  

    public async rejoin(joinOptions?: JoinOptions): Promise<void> {
        if (!this.isDisconected) {
            throw new Error(`Relay hub:${this.id} is connected`);
        }
        
        await this.relayServiceProxy._joinHubInternal((joinHubInfo) => {
            this.setJoinHubInfo(joinHubInfo);
            return this;
        }, this.id, this.selfParticipant!.properties, joinOptions || {});
    }

    public sendData(sendOption: SendOption, targetParticipants: string[] | null, type: string, data: Uint8Array): Promise<void> {
        const logger = this.relayServiceProxy.logger;
        if (logger) {
            logger.log(LogLevel.Debug, `sendData -> hubId:${this.id} option:${sendOption} target:${JSON.stringify(targetParticipants)} type:${type} length:${data.length}`);
        }

        const dataArray = Array.from(data);
        const sendHubData: SendHubData = {
            hubId: this.id,
            sendOption,
            targetParticipantIds: targetParticipants || null,
            type,
        };

        return this.relayServiceProxy.send('SendDataHubExAsync', keysToPascal(sendHubData), dataArray);
    }

    public dispose(): Promise<void> {
        const logger = this.relayServiceProxy.logger;
        if (logger) {
            logger.log(LogLevel.Debug, `leaving -> hubId:${this.id}`);
        }

        return this.relayServiceProxy.send('LeaveHubAsync', this.id);
    }

    public async _OnDeleted(): Promise<void> {      
        for (const callback of this.hubDeletedCallbacks.items) {
            await callback();
        }        
    }

    public async _OnDisconnected(): Promise<void> {     
        this.isDisconected = true; 
        for (const callback of this.hubDisconnectedCallbacks.items) {
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

            for (const callback of this.receiveDataCallbacks.items) {
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

            for (const callback of this.participantChangedCallbacks.items) {
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
