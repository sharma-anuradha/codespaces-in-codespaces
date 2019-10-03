import { ConfigurationManager } from './configuration-manager';
import { ConnectionManager } from './connection-manager';
import { CredentialsManager } from './credentials-manager';
import { LiveShareConnectionFactory } from './connection-factory';
import { IHttpClient } from './http-client';
import { ILiveShareClient } from '../../ts-agent/client/ILiveShareClient';
import { createLogger, Logger } from './logger';

export class ServiceRegistry {
    private instances = new Map<string, unknown>();
    private factories = new Map<string, (serviceRegistry: ServiceRegistry) => unknown>();
    private singletons = new Map<string, unknown>();
    private logger: Logger;

    constructor() {
        this.logger = createLogger('ServiceRegistry');
    }

    canResolve(serviceHandle: string): boolean {
        return this.instances.has(serviceHandle) || this.factories.has(serviceHandle);
    }

    unregisterInstance(serviceHandle: string) {
        this.instances.delete(serviceHandle);
        this.singletons.delete(serviceHandle);
    }

    getInstance(serviceHandle: 'CredentialsManager', useSingleton?: boolean): CredentialsManager;
    getInstance(
        serviceHandle: 'ConnectionFactory',
        useSingleton?: boolean
    ): LiveShareConnectionFactory;
    getInstance(serviceHandle: 'ConnectionManager', useSingleton?: boolean): ConnectionManager;
    getInstance(
        serviceHandle: 'ConfigurationManager',
        useSingleton?: boolean
    ): ConfigurationManager;
    getInstance(serviceHandle: 'LiveShareClient', useSingleton?: boolean): ILiveShareClient;
    getInstance(serviceHandle: 'HttpClient', useSingleton?: boolean): IHttpClient;
    getInstance(serviceHandle: string, useSingleton = true): unknown {
        if (this.instances.has(serviceHandle)) {
            return this.instances.get(serviceHandle);
        }

        if (useSingleton && this.singletons.has(serviceHandle)) {
            return this.singletons.get(serviceHandle);
        }

        if (this.factories.has(serviceHandle)) {
            const factory = this.factories.get(serviceHandle)!;
            try {
                const instance = factory(this);
                this.singletons.set(serviceHandle as any, instance as any);

                return instance;
            } catch (error) {
                this.logger.error('Could not create new instance of service.', {
                    serviceHandle,
                    error,
                });
                throw error;
            }
        }
        throw new Error(`Service not registered: ${serviceHandle}`);
    }
    registerInstance(serviceHandle: 'CredentialsManager', service: CredentialsManager): void;
    registerInstance(serviceHandle: 'CredentialsManager', service: CredentialsManager): void;
    registerInstance(serviceHandle: 'ConnectionFactory', service: LiveShareConnectionFactory): void;
    registerInstance(serviceHandle: 'ConnectionManager', service: ConnectionManager): void;
    registerInstance(serviceHandle: 'ConfigurationManager', service: ConfigurationManager): void;
    registerInstance(serviceHandle: 'LiveShareClient', service: ILiveShareClient): void;
    registerInstance(serviceHandle: 'HttpClient', service: IHttpClient): void;
    registerInstance(serviceHandle: string, instance: unknown) {
        this.instances.set(serviceHandle, instance);
    }
    registerFactory(
        serviceHandle: 'CredentialsManager',
        factory: (serviceRegistry: ServiceRegistry) => CredentialsManager
    ): void;
    registerFactory(
        serviceHandle: 'ConnectionFactory',
        factory: (serviceRegistry: ServiceRegistry) => LiveShareConnectionFactory
    ): void;
    registerFactory(
        serviceHandle: 'ConnectionManager',
        factory: (serviceRegistry: ServiceRegistry) => ConnectionManager
    ): void;
    registerFactory(
        serviceHandle: 'ConfigurationManager',
        factory: (serviceRegistry: ServiceRegistry) => ConfigurationManager
    ): void;
    registerFactory(
        serviceHandle: 'LiveShareClient',
        factory: (serviceRegistry: ServiceRegistry) => ILiveShareClient
    ): void;
    registerFactory(
        serviceHandle: 'HttpClient',
        factory: (serviceRegistry: ServiceRegistry) => IHttpClient
    ): void;
    registerFactory(serviceHandle: string, factory: (serviceRegistry: ServiceRegistry) => unknown) {
        this.factories.set(serviceHandle, factory);
    }
}
