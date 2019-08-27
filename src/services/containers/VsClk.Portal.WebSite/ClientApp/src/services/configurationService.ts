import { useWebClient } from '../actions/middleware/useWebClient';

export interface IConfiguration {
    environmentRegistrationEndpoint: string;
}

export const defaultConfig: IConfiguration = {
    environmentRegistrationEndpoint: '/api/environment/registration',
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
