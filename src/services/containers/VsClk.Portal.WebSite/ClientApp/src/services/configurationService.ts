import { useWebClient } from '../actions/middleware/useWebClient';


export type TEnvironment = 'development' | 'local' | 'staging' | 'production';

export interface IConfiguration {
    readonly portalEndpoint: string;
    readonly environmentRegistrationEndpoint: string;
    readonly apiEndpoint: string;
    readonly liveShareEndpoint: string;
    readonly liveShareWebExtensionEndpoint: string;
    readonly environment: TEnvironment;
    readonly portForwardingDomainTemplate: string;
    readonly enableEnvironmentPortForwarding: boolean;
    readonly environmentsApiPath: string;
}

export const defaultConfig: IConfiguration = {
    portalEndpoint: 'https://online.visualstudio.com/',
    environmentRegistrationEndpoint: 'https://online.visualstudio.com/api/v1/environments',
    apiEndpoint: 'https://online.visualstudio.com/api/v1',
    liveShareEndpoint: 'https://prod.liveshare.vsengsaas.visualstudio.com',
    liveShareWebExtensionEndpoint: 'https://vslsprod.blob.core.windows.net/webextension',
    environment: 'production',
    portForwardingDomainTemplate: '{0}.app.online.visualstudio.com',
    enableEnvironmentPortForwarding: false,
    environmentsApiPath: '/api/v1/environments'
};

export const configurationEndpoint = '/configuration';
export async function getServiceConfiguration(): Promise<IConfiguration> {
    const webClient = useWebClient();

    const config = await webClient.request<IConfiguration>(
        configurationEndpoint,
        {},
        { requiresAuthentication: false }
    );

    return {
        ...defaultConfig,
        ...config,
    };
}


