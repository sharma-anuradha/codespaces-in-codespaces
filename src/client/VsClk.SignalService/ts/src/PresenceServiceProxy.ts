import * as signalR from '@aspnet/signalr';
import { IPresenceServiceProxy, ConnectionChangeType, IContactReference }  from './IPresenceServiceProxy';
import { HubProxyBase } from './HubProxyBase';

export class PresenceServiceProxy extends HubProxyBase implements IPresenceServiceProxy {
    private updatePropertiesCallbacks: Array<(contact: IContactReference, properties: { [key: string]: any; }, targetConnectionId: string) => void>;
    private receiveMessageCallbacks: Array<(targetContact: IContactReference, fromContact: IContactReference, messageType: string, body: any) => void>;
    private connectionChangedCallbacks: Array<(contact: IContactReference, changeType: ConnectionChangeType) => void>;

    constructor(
        hubConnection: signalR.HubConnection,
        logger?: signalR.ILogger,
        useSignalRHub?: boolean) {
        super(hubConnection, logger, useSignalRHub ? 'presenceServiceHub' : undefined)

        hubConnection.on(this.toHubMethodName('updateValues'), (contact, properties, targetConnectionId) => this.updateValues(contact, properties, targetConnectionId));
        hubConnection.on(this.toHubMethodName('receiveMessage'), (targetContact, fromContact, messageType, body) => this.receiveMessage(targetContact, fromContact, messageType, body));
        hubConnection.on(this.toHubMethodName('connectionChanged'), (contact, changeType) => this.connectionChanged(contact, changeType));

        this.updatePropertiesCallbacks = [];
        this.receiveMessageCallbacks = [];
        this.connectionChangedCallbacks = [];
    }

    public onUpdateProperties(callback: (contact: IContactReference, properties: { [key: string]: any; }, targetConnectionId: string) => void): void {
        if (callback) {
            this.updatePropertiesCallbacks.push(callback);
        }
    }

    public onMessageReceived(callback: (targetContact: IContactReference, fromContact: IContactReference, messageType: string, body: any) => void): void {
        if (callback) {
            this.receiveMessageCallbacks.push(callback);
        }
    }

    public onConnectionChanged(callback: (contact: IContactReference, changeType: ConnectionChangeType) => void): void {
        if (callback) {
            this.connectionChangedCallbacks.push(callback);
        }
    }

    public async registerSelfContact(contactId: string, initialProperties: { [key: string]: any; }): Promise<{ [key: string]: any; }> {
        const registerProperties = await this.invoke<{ [key: string]: any; }>('RegisterSelfContactAsync', contactId, initialProperties);
        return registerProperties;
    }

    public publishProperties(updateProperties: { [key: string]: any; }): Promise<void> {
        return this.send('PublishPropertiesAsync', updateProperties);
    }

    public sendMessage(targetContact: IContactReference, messageType: string, body: any): Promise<void> {
        return this.send('SendMessageAsync', targetContact, messageType, body);
    }

    public async addSubcriptions(targetContacts: IContactReference[] , propertyNames: string[]): Promise<{ [key: string]: { [key: string]: any; }; }> {
        return this.invoke<{ [key: string]: { [key: string]: any; }; }>('AddSubcriptionsAsync', targetContacts, propertyNames);
    }

    public requestSubcriptions(targetContactProperties: { [key: string]: any; }[], propertyNames: string[], useStubContact: boolean): Promise<{ [key: string]: any; }[]> {
        return this.invoke<{ [key: string]: any; }[]>('RequestSubcriptionsAsync)', targetContactProperties, propertyNames, useStubContact);
    }

    public removeSubscription(targetContacts: IContactReference[]): Promise<void> {
        return this.send('RemoveSubscription', targetContacts);
    }

    public unregisterSelfContact(): Promise<void> {
        return this.send('UnregisterSelfContactAsync');
    }

    private updateValues(contact: IContactReference, properties: { [key: string]: any; }, targetConnectionId: string): void {
        if (this.logger) {
            this.logger.log(signalR.LogLevel.Debug, `PresenceServiceProxy.updateValues contact:${JSON.stringify(contact)} properties:${JSON.stringify(properties)}`);
        }

        this.updatePropertiesCallbacks.forEach(c => c(contact, properties, targetConnectionId));
    }

    private receiveMessage(targetContact: IContactReference, fromContact: IContactReference, messageType: string, body: any): void {
        if (this.logger) {
            this.logger.log(signalR.LogLevel.Debug, `PresenceServiceProxy.receiveMessage targetContact:${JSON.stringify(targetContact)} fromContact:${JSON.stringify(fromContact)} messageType:${messageType} body:${JSON.stringify(body)}`);
        }

        this.receiveMessageCallbacks.forEach(c => c(targetContact, fromContact, messageType, body));
    }

    private connectionChanged(contact: IContactReference, changeType: ConnectionChangeType): void {
        if (this.logger) {
            this.logger.log(signalR.LogLevel.Debug, `PresenceServiceProxy.connectionChanged contact:${JSON.stringify(contact)} changeType:${changeType}`);
        }

        this.connectionChangedCallbacks.forEach(c => c(contact, changeType));
    }
}