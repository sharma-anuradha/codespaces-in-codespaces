import { AuthenticationError, IAuthenticationProvider } from './authService';
import {
    ICloudEnvironment,
    CreateEnvironmentParameters as CreateEnvironmentParametersBase,
} from '../interfaces/cloudenvironment';
import { getServiceConfiguration } from './configurationService';

import { createUniqueId } from '../dependencies';

// Webpack configuration enforces isolatedModules use on typescript
// and prevents direct re-exporting of types.
export type CreateEnvironmentParameters = CreateEnvironmentParametersBase;

export default class EnvRegService {
    private static async get(
        url: string,
        authenticationProvider: IAuthenticationProvider
    ): Promise<Response | undefined> {
        const token = await authenticationProvider.getToken();

        if (!token) {
            await authenticationProvider.signOut();
            throw new AuthenticationError();
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
            await authenticationProvider.signOut();
            throw new AuthenticationError();
        }

        if (response.status !== 200) {
            const error = new Error(response.statusText);
            (error as any).code = response.status;
            throw error;
        }

        return response;
    }

    private static async post(
        url: string,
        data: any,
        authenticationProvider: IAuthenticationProvider
    ): Promise<Response | undefined> {
        const token = await authenticationProvider.getToken();

        if (!token) {
            await authenticationProvider.signOut();
            throw new AuthenticationError();
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
            await authenticationProvider.signOut();
            throw new AuthenticationError();
        }

        if (response.status !== 200) {
            throw new Error(response.statusText);
        }

        return response;
    }

    private static async delete(
        url: string,
        authenticationProvider: IAuthenticationProvider
    ): Promise<Response | undefined> {
        const token = await authenticationProvider.getToken();

        if (!token) {
            await authenticationProvider.signOut();
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
            await authenticationProvider.signOut();
            return undefined;
        }

        if (response.status !== 204) {
            throw new Error(response.statusText);
        }

        return response;
    }

    static async fetchEnvironments(
        authenticationProvider: IAuthenticationProvider
    ): Promise<ICloudEnvironment[]> {
        const emptyEnvironmentList: ICloudEnvironment[] = [];
        const { environmentRegistrationEndpoint } = await getServiceConfiguration();

        const response = await this.get(environmentRegistrationEndpoint, authenticationProvider);
        if (!response) {
            return emptyEnvironmentList;
        }
        const fetchedEnvironments = await response.json();
        if (!Array.isArray(fetchedEnvironments)) {
            return emptyEnvironmentList;
        }

        fetchedEnvironments.forEach((environment) => {
            environment.active = new Date(environment.active);
            environment.created = new Date(environment.created);
            environment.updated = new Date(environment.updated);
        });

        return fetchedEnvironments.sort((a: ICloudEnvironment, b: ICloudEnvironment) => {
            return b.updated.getTime() - a.updated.getTime();
        });
    }

    static async createEnvironment(
        environment: CreateEnvironmentParameters,
        authenticationProvider: IAuthenticationProvider
    ): Promise<ICloudEnvironment> {
        const { environmentRegistrationEndpoint } = await getServiceConfiguration();

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

        const response = await this.post(
            environmentRegistrationEndpoint,
            body,
            authenticationProvider
        );

        if (!response) {
            throw new Error('Service returned no data.');
        }

        return await response.json();
    }

    static async getEnvironment(
        id: string,
        authenticationProvider: IAuthenticationProvider
    ): Promise<ICloudEnvironment | undefined> {
        const { environmentRegistrationEndpoint } = await getServiceConfiguration();

        const response = await this.get(
            `${environmentRegistrationEndpoint}/${id}`,
            authenticationProvider
        );

        if (!response) {
            return undefined;
        }

        return await response.json();
    }

    static async deleteEnvironment(
        id: string,
        authenticationProvider: IAuthenticationProvider
    ): Promise<void> {
        const { environmentRegistrationEndpoint } = await getServiceConfiguration();

        const response = await this.delete(
            `${environmentRegistrationEndpoint}/${id}`,
            authenticationProvider
        );
        if (!response || !response.ok === true || response.status !== 204) {
            throw new Error(`Failed to delete environment ${id}. StatusCode: ${204}`);
        }
    }
}
