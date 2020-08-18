import { TKnownPartners } from '../interfaces/TKnownPartners';
import { ConfigurationError } from '../errors/ConfigurationError';
import { IEnvironment, isHostedOnGithub } from '../../../vso-client-core/src';

interface IPartnerConfig {
    loginRedirectUrl: string;
}

export type TEnvironment = 'development' | 'local' | 'staging' | 'production';

interface ILocationConfig {
    readonly current: string;
    readonly available: ReadonlyArray<string>;
    readonly hostnames: Record<string, string>;
}

interface IConfig {
    readonly apiPath: string;
    partnerConfigs: Record<TKnownPartners, IPartnerConfig>;
    portalConfig: IConfiguration;
    locations: ILocationConfig;
}

export interface IConfiguration {
    readonly portalEndpoint: string;
    readonly environmentRegistrationEndpoint: string;
    readonly apiEndpoint: string;
    readonly liveShareEndpoint: string;
    readonly liveShareWebExtensionEndpoint: string;
    readonly environment: TEnvironment;
    readonly portForwardingDomainTemplate: string;
    readonly portForwardingManagementEndpoint: string;
    readonly enableEnvironmentPortForwarding: boolean;
    readonly portForwardingServiceEnabled: boolean;
    readonly environmentsApiPath: string;
    readonly richNavWebExtensionEndpoint: string;
    readonly isDevStamp: boolean;
    readonly githubApiEndpoint: string;
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
        githubApiEndpoint: 'https://api.github.com/vscs_internal/proxy',
        environment: 'production',
        portForwardingDomainTemplate: '{0}.app.online.visualstudio.com',
        enableEnvironmentPortForwarding: false,
        portForwardingServiceEnabled: false,
        environmentRegistrationEndpoint: 'https://online.visualstudio.com/api/v1/environments',
        richNavWebExtensionEndpoint: 'https://intellinavstgprod.blob.core.windows.net/webextension',
        portForwardingManagementEndpoint:
            'https://management.apps.codespaces.githubusercontent.com/api/v1/PortForwardingConnections',
        environmentsApiPath: '/api/v1/environments',
        isDevStamp: false,
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

const IS_FETCHED_SYMBOL = Symbol();
export class Config {
    private [IS_FETCHED_SYMBOL] = false;

    public get isFetched() {
        return this[IS_FETCHED_SYMBOL];
    }

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

        this[IS_FETCHED_SYMBOL] = true;
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

    /**
     * Partners need to have control over some of the API calls, for instance,
     * GitHub need to hook into the `start` codespace API call to prevent
     * a Codespace from being started if user is blocked or off-boarded.
     * This endpoint represents such proxied Codespaces API endpoint that
     * partners maintain.
     */
    public getProxiedApiEndpoint = (codespace: IEnvironment) => {
        if (isHostedOnGithub()) {
            return  CONFIG.portalConfig.githubApiEndpoint;
        }

        return this.getCodespaceRegionalApiEndpoint(codespace);
    }

    get environment() {
        return CONFIG.portalConfig.environment;
    }

    get liveShareApi() {
        return CONFIG.portalConfig.liveShareEndpoint;
    }

    get api() {
        return CONFIG.portalConfig.apiEndpoint;
    }

    get githubApi() {
        return CONFIG.portalConfig.githubApiEndpoint;
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

    get portForwardingManagementEndpoint() {
        return CONFIG.portalConfig.portForwardingManagementEndpoint;
    }
}

export const config = new Config();
