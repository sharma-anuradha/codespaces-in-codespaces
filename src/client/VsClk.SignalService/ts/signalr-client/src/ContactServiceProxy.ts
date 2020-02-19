import { IContactServiceProxy, ConnectionChangeType, IContactReference }  from './IContactServiceProxy';
import { HubProxyBase } from './HubProxyBase';
import { IHubProxy } from './IHubProxy';
import { LogLevel } from '@microsoft/signalr';
import { CallbackContainer } from './CallbackContainer';
import { IDisposable } from './IDisposable';

export class ContactServiceProxy extends HubProxyBase implements IContactServiceProxy {
    private updatePropertiesCallbacks = new CallbackContainer<(contact: IContactReference, properties: { [key: string]: any; }, targetConnectionId: string) => void>();
    private receiveMessageCallbacks = new CallbackContainer<(targetContact: IContactReference, fromContact: IContactReference, messageType: string, body: any) => void>();
    private connectionChangedCallbacks = new CallbackContainer<(contact: IContactReference, changeType: ConnectionChangeType) => void>();

    constructor(
        hubProxy: IHubProxy,
        logger?: signalR.ILogger,
        useSignalRHub?: boolean) {
        super(hubProxy, logger, useSignalRHub ? 'presenceServiceHub' : undefined);

        hubProxy.on(this.toHubMethodName('updateValues'), (contact, properties, targetConnectionId) => this.updateValues(contact, properties, targetConnectionId));
        hubProxy.on(this.toHubMethodName('receiveMessage'), (targetContact, fromContact, messageType, body) => this.receiveMessage(targetContact, fromContact, messageType, body));
        hubProxy.on(this.toHubMethodName('connectionChanged'), (contact, changeType) => this.connectionChanged(contact, changeType));
    }

    public onUpdateProperties(callback: (contact: IContactReference, properties: { [key: string]: any; }, targetConnectionId: string) => void): IDisposable {
        return this.updatePropertiesCallbacks.add(callback);
    }

    public onMessageReceived(callback: (targetContact: IContactReference, fromContact: IContactReference, messageType: string, body: any) => void): IDisposable {
        return this.receiveMessageCallbacks.add(callback);
    }

    public onConnectionChanged(callback: (contact: IContactReference, changeType: ConnectionChangeType) => void): IDisposable {
        return this.connectionChangedCallbacks.add(callback);
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
        return this.invoke<{ [key: string]: any; }[]>('RequestSubcriptionsAsync', targetContactProperties, propertyNames, useStubContact);
    }

    public removeSubscription(targetContacts: IContactReference[]): Promise<void> {
        return this.send('RemoveSubscription', targetContacts);
    }

    public unregisterSelfContact(): Promise<void> {
        return this.send('UnregisterSelfContactAsync');
    }

    private updateValues(contact: IContactReference, properties: { [key: string]: any; }, targetConnectionId: string): void {
        if (this.logger) {
            this.logger.log(LogLevel.Debug, `ContactServiceProxy.updateValues contact:${JSON.stringify(contact)} properties:${JSON.stringify(properties)}`);
        }

        this.updatePropertiesCallbacks.items.forEach(c => c(contact, properties, targetConnectionId));
    }

    private receiveMessage(targetContact: IContactReference, fromContact: IContactReference, messageType: string, body: any): void {
        if (this.logger) {
            this.logger.log(LogLevel.Debug, `ContactServiceProxy.receiveMessage targetContact:${JSON.stringify(targetContact)} fromContact:${JSON.stringify(fromContact)} messageType:${messageType} body:${JSON.stringify(body)}`);
        }

        this.receiveMessageCallbacks.items.forEach(c => c(targetContact, fromContact, messageType, body));
    }

    private connectionChanged(contact: IContactReference, changeType: ConnectionChangeType): void {
        if (this.logger) {
            this.logger.log(LogLevel.Debug, `ContactServiceProxy.connectionChanged contact:${JSON.stringify(contact)} changeType:${changeType}`);
        }

        this.connectionChangedCallbacks.items.forEach(c => c(contact, changeType));
    }
}