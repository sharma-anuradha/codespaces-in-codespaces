export interface IContactReference {
    readonly Id: string;
    readonly ConnectionId: string;
}

export declare enum ConnectionChangeType {
    None = 0,
    Added = 1,
    Removed = 2
}

export interface IContactServiceProxy {
    onUpdateProperties(callback: (contact: IContactReference, properties: { [key: string]: any; }, targetConnectionId: string) => void): void;

    onMessageReceived(callback: (targetContact: IContactReference, fromContact: IContactReference, messageType: string, body: any) => void): void;

    onConnectionChanged(callback: (contact: IContactReference, changeType: ConnectionChangeType) => void): void;

    registerSelfContact(contactId: string, initialProperties: { [key: string]: any; }): Promise<{ [key: string]: any; }>;

    publishProperties(updateProperties: { [key: string]: any; }): Promise<void>;

    sendMessage(targetContact: IContactReference, messageType: string, body: any): Promise<void>;

    addSubcriptions(targetContacts: IContactReference[] , propertyNames: string[]): Promise<{ [key: string]: { [key: string]: any; }; }>;

    requestSubcriptions(targetContactProperties: { [key: string]: any; }[], propertyNames: string[], useStubContact: boolean): Promise<{ [key: string]: any; }[]>;

    removeSubscription(targetContacts: IContactReference[]): Promise<void>;
 
    unregisterSelfContact(): Promise<void>; 
}