import { useWebClient } from '../actions/middleware/useWebClient';

export interface IConfiguration {
    environmentRegistrationEndpoint: string;
    liveShareEndpoint: string;
}

export const defaultConfig: IConfiguration = {
    environmentRegistrationEndpoint: 'https://online.visualstudio.com/api/v1/environments',
    liveShareEndpoint: 'https://prod.liveshare.vsengsaas.visualstudio.com',
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
