import { IServiceProxyBase } from './IServiceProxyBase';
import { RelayServiceProxy } from './RelayServiceProxy';

export interface IRelayHubParticipant {
    readonly id: string;
    readonly properties: { [key: string]: any; };
    readonly isSelf: boolean;
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
    readonly participants: IRelayHubParticipant[];
    readonly relayServiceProxy: IRelayServiceProxy;

    onReceiveData(callback: (receivedData: IReceivedData) => Promise<void>): void;
    onParticipantChanged(callback: (participantChanged: IParticipantChanged) => Promise<void>): void;
    onDeleted(callback: () => Promise<void>): void;

    sendData(sendOption: SendOption, targetParticipants: string[] | null, type: string, data: Uint8Array): Promise<void>;
}

export interface IRelayServiceProxy extends IServiceProxyBase {
    createHub(hubId?: string): Promise<string>;
    joinHub(hubId: string, properties: { [key: string]: any; }, createIfNotExists: boolean): Promise<IRelayHubProxy>;
    deleteHub(hubId: string): Promise<void>;
}