
import { IRelayServiceProxy, IRelayHubParticipant, IRelayHubProxy, IReceivedData, IParticipantChanged, ParticipantChangeType, SendOption, JoinOptions, SendHubData }  from './IRelayServiceProxy';
import { HubProxyBase, keysToPascal } from './HubProxyBase';
import { HubMethodOption, IHubProxy } from './IHubProxy';
import { CallbackContainer } from './CallbackContainer';
import { IDisposable } from './IDisposable';
import { ILogger, LogLevel } from './ILogger';

enum HubMethods {
    ReceiveData = 'receiveData',
    ParticipantChanged = 'participantChanged',
    HubDeleted = 'hubDeleted',

    CreateHub = 'CreateHubAsync',
    DeleteHub = 'DeleteHubAsync',
    JoinHub = 'JoinHubAsync',
    SendDataHubEx = 'SendDataHubExAsync',
    LeaveHub = 'LeaveHubAsync',
};

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

        hubProxy.on(this.toHubMethodName(HubMethods.ReceiveData), async (hubId, fromParticipantId, uniqueId, type, data, properties) => {
            let bufferData: Uint8Array;
            if (typeof data === 'string') {
                bufferData = isNode ? Buffer.from(data, 'base64') : Base64Binary.decode(data);
            } else {
                bufferData = data;
            }

            if (this.logger && this.traceHubData) {
                this.logger.log(LogLevel.Debug, `receiveData -> hubId:${hubId} fromParticipantId:${fromParticipantId} type:${type} length:${bufferData ? bufferData.length: 0} properties:${JSON.stringify(properties)}`);
            }

            const relayHub = this.relayHubs.get(hubId);
            if (relayHub) {
                await relayHub._OnReceiveData(fromParticipantId, uniqueId, type, <Uint8Array> bufferData, properties);
            }
        });

        hubProxy.on(this.toHubMethodName(HubMethods.ParticipantChanged), async (hubId, participantId, properties, changeType) => {
            if (this.logger) {
                this.logger.log(LogLevel.Debug, `participantChanged -> hubId:${hubId} participantId:${participantId} properties:${JSON.stringify(properties)} changeType:${changeType}`);
            }

            const relayHub = this.relayHubs.get(hubId);
            if (relayHub) {
                await relayHub._OnParticipantChanged(participantId, properties, changeType);
            }
        });

        hubProxy.on(this.toHubMethodName(HubMethods.HubDeleted), async (hubId) => {
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

    public traceHubData: boolean = false;

    public createHub(hubId?: string): Promise<string> {
        return this.invoke<string>(HubMethods.CreateHub, hubId);
    }

    public async joinHub(hubId: string, properties: { [key: string]: any; }, joinOptions: JoinOptions): Promise<IRelayHubProxy> {
        let relayHub = this.relayHubs.get(hubId);

        if (relayHub) {
            if (relayHub.isDisconected) {
                await relayHub.rejoin() 
            }
        } else {
            relayHub = await this._joinHubInternal(
                (joinHubInfo) => new RelayHubProxy(this, hubId, joinHubInfo),
                hubId,
                properties,
                joinOptions);
        }

        return relayHub;
    }

    public async deleteHub(hubId: string): Promise<void> {
        await this.invoke(HubMethods.DeleteHub, hubId);
    }

    public async _joinHubInternal( relayHubProxyFactory: (joinHubInfo: JoinHubInfo) => RelayHubProxy, hubId: string, properties: { [key: string]: any; }, joinOptions: JoinOptions): Promise<RelayHubProxy> {
        const joinHubInfo = await this.invokeKeysToCamel<JoinHubInfo>(HubMethods.JoinHub, hubId, properties, keysToPascal(joinOptions));
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
    private hubParticipants = new Map<string, IRelayHubParticipant>();

    constructor(
        public readonly relayServiceProxy: RelayServiceProxy,
        public id: string,
        joinHubInfo: JoinHubInfo) {
        this.setJoinHubInfo(joinHubInfo);
    }

    public isDisconected: boolean = false;

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

    public async sendData(sendOption: SendOption, targetParticipants: string[] | null, type: string, data: Uint8Array, properties?: { [key: string]: any; }, methodOption?: HubMethodOption): Promise<number> {
        const logger = this.relayServiceProxy.logger;
        if (logger  && this.relayServiceProxy.traceHubData ) {
            logger.log(LogLevel.Debug, `sendData -> hubId:${this.id} option:${sendOption} target:${JSON.stringify(targetParticipants)} type:${type} length:${data ? data.length : 0} properties:${JSON.stringify(properties)}`);
        }

        const dataArray = data ? Array.from(data) : null;
        const sendHubData: SendHubData = {
            hubId: this.id,
            sendOption,
            targetParticipantIds: targetParticipants || null,
            type,
        };

        const sendHubDataParam = keysToPascal(sendHubData);
        sendHubDataParam.messageProperties = properties;

        if (methodOption === HubMethodOption.Invoke) {
            return await this.relayServiceProxy.invoke<number>(HubMethods.SendDataHubEx, sendHubDataParam, dataArray);
        }

        await this.relayServiceProxy.send(HubMethods.SendDataHubEx, sendHubDataParam, dataArray);
        return 0;
    }

    public dispose(): Promise<void> {
        const logger = this.relayServiceProxy.logger;
        if (logger) {
            logger.log(LogLevel.Debug, `leaving -> hubId:${this.id}`);
        }

        return this.relayServiceProxy.send(HubMethods.LeaveHub, this.id);
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

    public async _OnReceiveData(fromParticipantId: string, uniqueId: number, type: string , data: Uint8Array, properties: { [key: string]: any; } ): Promise<void> {
        
        const participant = this.hubParticipants.get(fromParticipantId);
        if (participant) {
            const receivedData: IReceivedData = {
                fromParticipant: participant,
                uniqueId,
                type,
                data,
                properties
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

const Base64Binary = {
	_keyStr : "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=",
	
	/* will return a  Uint8Array type */
	decodeArrayBuffer: function(input: string) {
		var bytes = parseInt(<any>((input.length/4) * 3), 10);
		var ab = new ArrayBuffer(bytes);
		this.decode(input, ab);
		
		return ab;
	},

	removePaddingChars: function(input: string){
		var lkey = this._keyStr.indexOf(input.charAt(input.length - 1));
		if(lkey == 64){
			return input.substring(0,input.length - 1);
		}
		return input;
	},

	decode: function (input: string, arrayBuffer?: ArrayBuffer) {
		//get last chars to see if are valid
		input = this.removePaddingChars(input);
		input = this.removePaddingChars(input);

		var bytes = (input.length / 4) * 3;
		
		var uarray;
		var chr1, chr2, chr3;
		var enc1, enc2, enc3, enc4;
		var i = 0;
		var j = 0;
		
		if (arrayBuffer)
			uarray = new Uint8Array(arrayBuffer);
		else
			uarray = new Uint8Array(bytes);
		
		input = input.replace(/[^A-Za-z0-9\+\/\=]/g, "");
		
		for (i=0; i<bytes; i+=3) {	
			//get the 3 octects in 4 ascii chars
			enc1 = this._keyStr.indexOf(input.charAt(j++));
			enc2 = this._keyStr.indexOf(input.charAt(j++));
			enc3 = this._keyStr.indexOf(input.charAt(j++));
			enc4 = this._keyStr.indexOf(input.charAt(j++));
	
			chr1 = (enc1 << 2) | (enc2 >> 4);
			chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
			chr3 = ((enc3 & 3) << 6) | enc4;
	
			uarray[i] = chr1;			
			if (enc3 != 64) uarray[i+1] = chr2;
			if (enc4 != 64) uarray[i+2] = chr3;
		}
	
		return uarray;	
	}
}