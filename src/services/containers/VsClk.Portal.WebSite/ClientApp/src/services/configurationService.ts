import { useWebClient } from '../actions/middleware/useWebClient';

export type TEnvironment = 'development' | 'local' | 'staging' | 'production';

export interface IConfiguration {
    portalEndpoint: string;
    environmentRegistrationEndpoint: string;
    apiEndpoint: string;
    liveShareEndpoint: string;
    liveShareWebExtensionEndpoint: string;
    environment: TEnvironment;
}

export const defaultConfig: IConfiguration = {
    portalEndpoint: 'https://online.visualstudio.com/',
    environmentRegistrationEndpoint: 'https://online.visualstudio.com/api/v1/environments',
    apiEndpoint: 'https://online.visualstudio.com/api/v1',
    liveShareEndpoint: 'https://prod.liveshare.vsengsaas.visualstudio.com',
    liveShareWebExtensionEndpoint: 'https://vslsprod.blob.core.windows.net/webextension',
    environment: 'production',
};

export const configurationEndpoint = '/configuration';
export async function getServiceConfiguration(): Promise<IConfiguration> {
    const webClient = useWebClient();

    const config = await webClient.request<IConfiguration>(
        configurationEndpoint,
        {},
        { requiresAuthentication: false }
    );

    return config;
}
