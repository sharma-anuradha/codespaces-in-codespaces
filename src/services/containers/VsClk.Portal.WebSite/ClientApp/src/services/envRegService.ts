import { authService, IToken } from './authService';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import { getServiceConfiguration } from './configurationService';

export default class EnvRegService {
    private static async get(url: string): Promise<Response> {
        const token = await authService.getCachedToken();

        if (token) {
            const response = await fetch(url, {
                method: 'GET',
                redirect: 'follow',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token.accessToken}`,
                },
            });

            if (!response) {
                throw new Error(`GET to ${url} with returned an empty response`);
            }
            if (response.status !== 200) {
                const error = new Error(response.statusText);
                (error as any).code = response.status;
                throw error;
            }
            return response;
        }
        return undefined;
    }

    private static async post(url: string, data: any): Promise<Response> {
        const token = await authService.getCachedToken();
        if (token) {
            return fetch(url, {
                method: 'POST',
                credentials: 'include',
                redirect: 'follow',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token.accessToken}`,
                },
                body: JSON.stringify(data),
            }).then((response) => {
                if (!response) {
                    throw new Error(
                        `POST to ${url} with data ${JSON.stringify(
                            data
                        )} returned an empty response`
                    );
                }
                if (response.status !== 200) {
                    throw new Error(response.statusText);
                }
                return response;
            });
        }
        return undefined;
    }

    static async fetchEnvironments(token?: IToken): Promise<ICloudEnvironment[]> {
        let env: ICloudEnvironment[] = [];

        if (!token) {
            token = await authService.getCachedToken();

            if (!token) {
                return [];
            }
        }

        const config = await getServiceConfiguration();

        return this.get(config.environmentRegistrationEndpoint)
            .then((response) => {
                return response.text().then((data) => {
                    return JSON.parse(data);
                });
            })
            .then((environments: ICloudEnvironment[]) => {
                if (environments) {
                    if (Array.isArray(environments)) {
                        // Convert dates.
                        environments.forEach((environment) => {
                            environment.active = new Date(environment.active);
                            environment.created = new Date(environment.created);
                            environment.updated = new Date(environment.updated);
                        });
                        env = environments;
                        // Order them by updated time DESC
                        environments.sort((a: ICloudEnvironment, b: ICloudEnvironment) => {
                            return b.updated.getTime() - a.updated.getTime();
                        });
                    }
                }
                return env;
            })
            .catch((e) => {
                console.error(e);
                throw e;
            });
    }

    static async newEnvironment(name: string): Promise<ICloudEnvironment> {
        const config = await getServiceConfiguration();

        return this.post(config.environmentRegistrationEndpoint, {
            friendlyName: name,
        })
            .then((response) => {
                return response.json();
            })
            .catch((e) => {
                throw 'Error creating new environment';
            });
    }

    static async getEnvironment(id: string): Promise<ICloudEnvironment> {
        const config = await getServiceConfiguration();

        return this.get(`${config.environmentRegistrationEndpoint}/${id}`).then((response) =>
            response.json()
        );
    }
}
