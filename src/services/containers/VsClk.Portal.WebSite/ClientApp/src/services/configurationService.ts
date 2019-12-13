import { useWebClient } from '../actions/middleware/useWebClient';

export interface IConfiguration {
    portalEndpoint: string;
    environmentRegistrationEndpoint: string;
    apiEndpoint: string;
    liveShareEndpoint: string;
    liveShareWebExtensionEndpoint: string;
}

export const defaultConfig: IConfiguration = {
    portalEndpoint: 'https://online.visualstudio.com/',
    environmentRegistrationEndpoint: 'https://online.visualstudio.com/api/v1/environments',
    apiEndpoint: 'https://online.visualstudio.com/api/v1',
    liveShareEndpoint: 'https://prod.liveshare.vsengsaas.visualstudio.com',
    liveShareWebExtensionEndpoint: 'https://vslswebextension.blob.core.windows.net/vslsweb',
};

export const configurationEndpoint = '/configuration';
export async function getServiceConfiguration(): Promise<IConfiguration> {
    const webClient = useWebClient();

    return await webClient.request<IConfiguration>(
        configurationEndpoint,
        {},
        { requiresAuthentication: false }
    );
}
