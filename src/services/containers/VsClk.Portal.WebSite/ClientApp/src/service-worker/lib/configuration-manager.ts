import { ServiceWorkerConfiguration } from '../../common/service-worker-configuration';
import { createLogger, Logger } from './logger';
import { CriticalError } from './errors/CriticalError';

export class ConfigurationManager {
    private readonly logger: Logger;

    private currentConfiguration?: ServiceWorkerConfiguration;

    constructor() {
        this.logger = createLogger('ConfigurationManager');
    }

    get configuration(): ServiceWorkerConfiguration {
        if (!this.currentConfiguration) {
            throw new CriticalError('NotInitialized');
        }

        return this.currentConfiguration;
    }

    getConfigurationSafe() {
        return this.currentConfiguration || null;
    }

    updateConfiguration(configuration: ServiceWorkerConfiguration) {
        this.currentConfiguration = configuration;

        this.logger.info('Updated service worker configuration', {
            configuration: this.configuration,
        });
    }
}
