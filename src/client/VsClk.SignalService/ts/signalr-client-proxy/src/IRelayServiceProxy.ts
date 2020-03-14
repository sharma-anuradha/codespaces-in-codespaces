import { IServiceProxyBase } from './IServiceProxyBase';
import { IDisposable } from './IDisposable';
import { HubMethodOption } from './IHubProxy';

export interface IRelayHubParticipant {
    readonly id: string;
    readonly properties: { [key: string]: any; };
}

export interface IReceivedData {
    readonly fromParticipant: IRelayHubParticipant;
    readonly uniqueId: number;
    readonly type: string;
    readonly data: Uint8Array;
    readonly properties: { [key: string]: any; };
}

export enum ParticipantChangeType {
    None = 0,
    Added = 1,
    Removed = 2,
    Updated = 3
}

export enum SendOption {
    None,
    ExcludeSelf
}

export interface IParticipantChanged {
    readonly participant: IRelayHubParticipant;
    readonly changeType: ParticipantChangeType;
}

export interface IRelayDataHubProxy {
    onReceiveData(callback: (receivedData: IReceivedData) => Promise<void>): IDisposable;
}

export interface IRelayHubProxy extends IRelayDataHubProxy {
    readonly serviceId: string;
    readonly stamp: string;
    readonly id: string;
    readonly selfParticipant: IRelayHubParticipant;
    readonly participants: IRelayHubParticipant[];
    readonly relayServiceProxy: IRelayServiceProxy;

    onParticipantChanged(callback: (participantChanged: IParticipantChanged) => Promise<void>): IDisposable;
    onDeleted(callback: () => Promise<void>): IDisposable;
    onDisconnected(callback: () => Promise<void>): IDisposable;

    sendData(sendOption: SendOption, targetParticipants: string[] | null, type: string, data: Uint8Array, properties?: { [key: string]: any; }, methodOption?: HubMethodOption): Promise<number>;
    rejoin(joinOptions?: JoinOptions): Promise<void>;
    dispose(): Promise<void>;
}

export interface JoinOptions {
    readonly createIfNotExists?: boolean;
}

export interface SendHubData {
    readonly hubId: string;
    readonly sendOption: number;
    readonly targetParticipantIds?: string[] | null;
    readonly type: string;
    readonly messageProperties?: { [key: string]: any; };
}

export interface IRelayServiceProxy extends IServiceProxyBase {
    traceHubData: boolean;
    createHub(hubId?: string): Promise<string>;
    joinHub(hubId: string, properties: { [key: string]: any; }, joinOptions: JoinOptions): Promise<IRelayHubProxy>;
    deleteHub(hubId: string): Promise<void>;
}