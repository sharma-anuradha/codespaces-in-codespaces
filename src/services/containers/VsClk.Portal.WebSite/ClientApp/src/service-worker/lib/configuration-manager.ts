import { VSLS_API_URI } from '../../constants';
import { ServiceWorkerConfiguration } from '../../common/service-worker-configuration';
import { createLogger, Logger } from './logger';

export class ConfigurationManager {
    private readonly logger: Logger;

    private currentConfiguration: ServiceWorkerConfiguration = {
        liveShareEndpoint: VSLS_API_URI,
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
