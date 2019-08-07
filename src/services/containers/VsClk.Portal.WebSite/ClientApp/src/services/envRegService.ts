import { authService, IToken } from './authService';
import {
    ICloudEnvironment,
    CreateEnvironmentParameters as CreateEnvironmentParametersBase,
} from '../interfaces/cloudenvironment';
import { getServiceConfiguration } from './configurationService';
import { trace } from '../utils/trace';

import { createUniqueId } from '../dependencies';

// Webpack configuration enforces isolatedModules use on typescript
// and prevents direct re-exporting of types.
export type CreateEnvironmentParameters = CreateEnvironmentParametersBase;

export default class EnvRegService {
    private static async get(url: string): Promise<Response | undefined> {
        const token = await authService.getCachedToken();

        if (!token) {
            await authService.signOut();
            return undefined;
        }

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

        if (response.status === 401) {
            await authService.signOut();
            return undefined;
        }

        if (response.status !== 200) {
            const error = new Error(response.statusText);
            (error as any).code = response.status;
            throw error;
        }

        return response;
    }

    private static async post(url: string, data: any): Promise<Response | undefined> {
        const token = await authService.getCachedToken();

        if (!token) {
            await authService.signOut();
            return undefined;
        }

        const response = await fetch(url, {
            method: 'POST',
            redirect: 'follow',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${token.accessToken}`,
            },
            body: JSON.stringify(data),
        });

        if (!response) {
            throw new Error(
                `POST to ${url} with data ${JSON.stringify(data)} returned an empty response`
            );
        }

        if (response.status === 401) {
            await authService.signOut();
            return undefined;
        }

        if (response.status !== 200) {
            throw new Error(response.statusText);
        }

        return response;
    }

    private static async delete(url: string): Promise<Response | undefined> {
        const token = await authService.getCachedToken();

        if (!token) {
            await authService.signOut();
            return undefined;
        }

        const response = await fetch(url, {
            method: 'DELETE',
            redirect: 'follow',
            headers: {
                Authorization: `Bearer ${token.accessToken}`,
            },
        });

        if (!response) {
            throw new Error(`DELETE to ${url} with data returned an empty response`);
        }

        if (response.status === 401) {
            await authService.signOut();
            return undefined;
        }

        if (response.status !== 204) {
            throw new Error(response.statusText);
        }

        return response;
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
                if (!response) {
                    return undefined;
                }
                return response.text().then((data) => {
                    return JSON.parse(data);
                });
            })
            .then((environments) => {
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
                trace(e);
                throw e;
            });
    }

    static async createEnvironment(
        environment: CreateEnvironmentParameters
    ): Promise<ICloudEnvironment> {
        const config = await getServiceConfiguration();

        const { friendlyName, gitRepositoryUrl, type = 'cloudEnvironment' } = environment;

        const body = {
            type,
            friendlyName,
            seed: {
                type: gitRepositoryUrl ? 'git' : '',
                moniker: gitRepositoryUrl ? gitRepositoryUrl : '',
                // TODO: Get git credentials for profile.
                gitConfig: {
                    userName: createUniqueId(),
                    userEmail: `test-cloudenv-${createUniqueId()}@outlook.com`,
                },
            },
        };

        return this.post(config.environmentRegistrationEndpoint, body)
            .then((response) => {
                if (!response) {
                    return undefined;
                }
                return response.json();
            })
            .catch((e) => {
                throw 'Error creating new environment';
            });
    }

    static async getEnvironment(id: string): Promise<ICloudEnvironment> {
        const config = await getServiceConfiguration();

        return this.get(`${config.environmentRegistrationEndpoint}/${id}`).then(
            (response) => response && response.json()
        );
    }

    static async deleteEnvironment(id: string): Promise<void> {
        const config = await getServiceConfiguration();

        return this.delete(`${config.environmentRegistrationEndpoint}/${id}`).then((response) => {
            if (!response || !response.ok === true || response.status !== 204) {
                throw new Error(`Failed to delete environment ${id}. StatusCode: ${204}`);
            }
        });
    }
}
