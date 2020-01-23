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

export declare enum ParticipantChangeType {
    None = 0,
    Added = 1,
    Removed = 2,
    Updated = 3
}

export declare enum SendOption {
    None,
    ExcludeSelf
}

export interface IParticipantChanged {
    readonly participant: IRelayHubParticipant;
    readonly changeType: ParticipantChangeType;
}

export interface IRelayHubProxy {
    readonly id: string;
    readonly participants: IRelayHubParticipant[];

    onReceiveData(callback: (receivedData: IReceivedData) => Promise<void>): void;
    onParticipantChanged(callback: (participantChanged: IParticipantChanged) => Promise<void>): void;

    sendData(sendOption: SendOption, targetParticipants: string[], type: string, data: Uint8Array): Promise<void>;
}

export interface IRelayServiceProxy {
    createHub(hubId?: string): Promise<string>;
    joinHub(hubId: string, properties: { [key: string]: any; }, createIfNotExists: boolean): Promise<IRelayHubProxy>;
}