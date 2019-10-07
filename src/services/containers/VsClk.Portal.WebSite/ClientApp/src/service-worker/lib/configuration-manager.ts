import { ServiceWorkerConfiguration } from '../../common/service-worker-configuration';
import { createLogger, Logger } from './logger';
import { defaultConfig } from '../../services/configurationService';

export class ConfigurationManager {
    private readonly logger: Logger;

    private currentConfiguration: ServiceWorkerConfiguration = {
        liveShareEndpoint: defaultConfig.liveShareEndpoint,
        features: {
            useSharedConnection: false,
        },
    };

    constructor() {
        this.logger = createLogger('ConfigurationManager');
    }

    get configuration(): ServiceWorkerConfiguration {
        return this.currentConfiguration;
    }

    updateConfiguration(configuration: Partial<ServiceWorkerConfiguration>) {
        this.currentConfiguration = {
            ...this.currentConfiguration,
            ...configuration,
        };

        this.logger.info('Updated service worker configuration', {
            configuration: this.configuration,
        });
    }
}
