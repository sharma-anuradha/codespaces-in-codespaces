import { TKnownPartners } from '../interfaces/TKnownPartners';
import { ConfigurationError } from '../errors/ConfigurationError';
import { IEnvironment } from '../../../vso-client-core/src';

interface IPartnerConfig {
    loginRedirectUrl: string;
}

export type TEnvironment = 'development' | 'local' | 'staging' | 'production';

interface ILocationConfig {
    readonly current: string;
    readonly available: Readonly<Array<string>>;
    readonly hostnames: Record<string, string>;
}

interface IConfig {
    readonly apiPath: string;
    partnerConfigs: Record<TKnownPartners, IPartnerConfig>;
    portalConfig: IConfiguration;
    locations: ILocationConfig;
}

export interface IConfiguration {
    portalEndpoint: string;
    apiEndpoint: string;
    liveShareEndpoint: string;
    liveShareWebExtensionEndpoint: string;
    environment: TEnvironment;
    portForwardingDomainTemplate: string;
    enableEnvironmentPortForwarding: boolean;
    portForwardingServiceEnabled: boolean;
}

const CONFIG: IConfig = {
    apiPath: '/api/v1',
    locations: {
        current: '',
        available: [],
        hostnames: {},
    },
    portalConfig: {
        portalEndpoint: 'https://online.visualstudio.com/',
        apiEndpoint: 'https://online.visualstudio.com/api/v1',
        liveShareEndpoint: 'https://prod.liveshare.vsengsaas.visualstudio.com',
        liveShareWebExtensionEndpoint: 'https://vslsprod.blob.core.windows.net/webextension',
        environment: 'production',
        portForwardingDomainTemplate: '{0}.app.online.visualstudio.com',
        enableEnvironmentPortForwarding: false,
        portForwardingServiceEnabled: false,
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

const fetchConfiguration = async () => {
    const result = await fetch('/configuration');
    if (!result || !result.ok) {
        throw new ConfigurationError('Cannot get portal configuration.');
    }

    const config = await result.json();

    return config;
};

const fetchLocations = async (apiEndpoint: string) => {
    const result = await fetch(`${apiEndpoint}/locations`);
    if (!result || !result.ok) {
        throw new ConfigurationError('Cannot locations.');
    }

    const config = await result.json();

    return config;
};

class Config {
    private isFetched = false;
    public props = CONFIG;

    public fetch = async () => {
        if (this.isFetched) {
            return;
        }

        // fetch config
        const config = await fetchConfiguration();
        CONFIG.portalConfig = {
            ...CONFIG.portalConfig,
            ...config,
        };

        // fetch location data
        const locations = await fetchLocations(this.api);
        CONFIG.locations = {
            ...locations,
        };

        this.isFetched = true;
    };

    public getCodespaceRegionalApiEndpoint = (codespace: IEnvironment) => {
        const { location } = codespace;
        if (!location) {
            throw new ConfigurationError('No `location` set on the codespace');
        }

        const { locations } = CONFIG;

        if (!locations.current) {
            throw new ConfigurationError('Fetch `locations` first.');
        }

        const { hostnames } = locations;
        if (!hostnames) {
            throw new ConfigurationError('No `hostnames` set on locations.');
        }

        const hostname = hostnames[location] || hostnames[location.toLowerCase()];
        if (!hostname) {
            throw new ConfigurationError(
                `No api hostname found for the codespace location: "${location}"`
            );
        }

        const apiUrl = new URL(CONFIG.apiPath, `https://${hostname}`);

        return apiUrl.toString();
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

    get enableEnvironmentPortForwarding() {
        return CONFIG.portalConfig.enableEnvironmentPortForwarding;
    }

    get portForwardingServiceEnabled() {
        return CONFIG.portalConfig.portForwardingServiceEnabled;
    }

    get portForwardingDomainTemplate() {
        return CONFIG.portalConfig.portForwardingDomainTemplate;
    }
}

export const config = new Config();
