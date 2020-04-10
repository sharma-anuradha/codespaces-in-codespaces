import { TKnownPartners } from '../interfaces/TKnownPartners';
import { ConfigurationError } from '../errors/ConfigurationError';

interface IPartnerConfig {
    loginRedirectUrl: string;
}

export type TEnvironment = 'development' | 'local' | 'staging' | 'production';

interface IConfig {
    partnerConfigs: Record<TKnownPartners, IPartnerConfig>;
    portalConfig: IConfiguration;
}

export interface IConfiguration {
    portalEndpoint: string;
    apiEndpoint: string;
    liveShareEndpoint: string;
    liveShareWebExtensionEndpoint: string;
    environment: TEnvironment;
}

const CONFIG: IConfig = {
    portalConfig: {
        portalEndpoint: 'https://online.visualstudio.com/',
        apiEndpoint: 'https://online.visualstudio.com/api/v1',
        liveShareEndpoint: 'https://prod.liveshare.vsengsaas.visualstudio.com',
        liveShareWebExtensionEndpoint: 'https://vslsprod.blob.core.windows.net/webextension',
        environment: 'production',
    },
    partnerConfigs: {
        github: {
            loginRedirectUrl: '',
        },
        salesforce: {
            loginRedirectUrl: '',
        },
        vso: {
            loginRedirectUrl: '',
        },
    },
} as const;

class Config {
    public props = CONFIG;

    public fetch = async () => {
        const result = await fetch('/configuration');
        if (!result || !result.ok) {
            throw new ConfigurationError('Cannot get portal configuration.');
        }

        const config = await result.json();

        CONFIG.portalConfig = {
            ...CONFIG.portalConfig,
            ...config,
        };
    };

    get environment() {
        return CONFIG.portalConfig.environment;
    }

    get liveShareApi() {
        return CONFIG.portalConfig.liveShareEndpoint;
    }

    get api() {
        return CONFIG.portalConfig.apiEndpoint;
    }
}

export const config = new Config();
