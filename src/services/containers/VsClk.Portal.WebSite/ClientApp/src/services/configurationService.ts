export interface IConfiguration {
    environmentRegistrationEndpoint: string;
}

const defaultConfig: IConfiguration = {
    environmentRegistrationEndpoint: '/api/environment/registration',
};

let configCache: IConfiguration | undefined;

export async function getServiceConfiguration(): Promise<IConfiguration> {
    if (configCache) {
        return configCache;
    }

    try {
        const response = await fetch('/configuration');

        if (response.status !== 200) {
            return defaultConfig;
        }

        configCache = (await response.json()) as IConfiguration;

        return configCache;
    } catch (err) {
        return defaultConfig;
    }
}
