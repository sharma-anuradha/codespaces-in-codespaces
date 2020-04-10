import { createLogger, Logger } from './logger';
import { ServiceWorkerConfiguration } from '../service-worker-configuration';
import { RequestStore } from '../../RequestStore';

export class ConfigurationManager {
    private readonly logger: Logger;
    private readonly storeKey = 'configuration';

    private readonly configurationRequestStore = new RequestStore<ServiceWorkerConfiguration>({
        defaultTimeout: 60 * 1000,
    });

    private currentConfiguration?: ServiceWorkerConfiguration;

    constructor() {
        this.logger = createLogger('ConfigurationManager');
    }

    async getConfiguration(): Promise<ServiceWorkerConfiguration> {
        if (this.currentConfiguration) {
            return this.currentConfiguration;
        }

        return this.configurationRequestStore.getResponse(this.storeKey);
    }

    getConfigurationSync() {
        return this.currentConfiguration || null;
    }

    updateConfiguration(configuration: ServiceWorkerConfiguration) {
        this.currentConfiguration = configuration;
        this.configurationRequestStore.setResponse(this.storeKey, configuration);

        this.logger.info('Updated service worker configuration', {
            configuration: this.currentConfiguration,
        });
    }
}
