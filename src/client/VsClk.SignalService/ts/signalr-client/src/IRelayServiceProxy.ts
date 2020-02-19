import { IServiceProxyBase } from './IServiceProxyBase';
import { IDisposable } from './IDisposable';

export interface IRelayHubParticipant {
    readonly id: string;
    readonly properties: { [key: string]: any; };
}

export interface IReceivedData {
    readonly fromParticipant: IRelayHubParticipant;
    readonly uniqueId: number;
    readonly type: string;
    readonly data: Uint8Array;
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

export interface IRelayHubProxy {
    readonly serviceId: string;
    readonly stamp: string;
    readonly id: string;
    readonly selfParticipant: IRelayHubParticipant;
    readonly participants: IRelayHubParticipant[];
    readonly relayServiceProxy: IRelayServiceProxy;

    onReceiveData(callback: (receivedData: IReceivedData) => Promise<void>): IDisposable;
    onParticipantChanged(callback: (participantChanged: IParticipantChanged) => Promise<void>): IDisposable;
    onDeleted(callback: () => Promise<void>): IDisposable;
    onDisconnected(callback: () => Promise<void>): IDisposable;

    sendData(sendOption: SendOption, targetParticipants: string[] | null, type: string, data: Uint8Array): Promise<void>;
    rejoin(joinOptions?: JoinOptions): Promise<void>;
}

export interface JoinOptions {
    readonly createIfNotExists?: boolean;
}

export interface IRelayServiceProxy extends IServiceProxyBase {
    createHub(hubId?: string): Promise<string>;
    joinHub(hubId: string, properties: { [key: string]: any; }, joinOptions: JoinOptions): Promise<IRelayHubProxy>;
    deleteHub(hubId: string): Promise<void>;
}