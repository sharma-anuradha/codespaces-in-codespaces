import { IEnvironment } from 'vso-client-core';

import { config } from '../config/config';
import { AuthenticationError } from '../errors/AuthenticationError';
import { RateLimitingError } from '../errors/ReteLimitingError';
import { HttpError } from '../errors/HttpError';

const cache: { [key: string]: Promise<IEnvironment> | undefined } = {};

const headerNames = {
    acceptRedirects: 'X-Can-Accept-Redirects',
};

export class VsoAPI {
    public getEnvironmentInfo = async (id: string, token: string): Promise<IEnvironment> => {
        const key = `${id}_${token}`;

        let currentRequest = cache[key];
        if (currentRequest) {
            return await currentRequest;
        }

        try {
            currentRequest = this.getEnvironmentInfoInternal(id, token);
            cache[key] = currentRequest;

            return await currentRequest;
        } finally {
            delete cache[key];
        }
    };

    private getEnvironmentInfoInternal = async (id: string, token: string) => {
        const url = `${config.api}/environments/${id}?t=${Date.now()}`;
        const headers = {
            Authorization: `Bearer ${token}`,
            [headerNames.acceptRedirects]: 'false',
        };

        let environmentInfoResponse = await fetch(url, { headers });

        if (environmentInfoResponse.status === 333) {
            const { location: redirectLocation } = await environmentInfoResponse.json();
            if (redirectLocation) {
                environmentInfoResponse = await fetch(redirectLocation, { headers });
            }
        }

        if (!environmentInfoResponse.ok) {
            const message = 'Cannot fetch Codespace info';

            if (environmentInfoResponse.status === 401) {
                throw new AuthenticationError(message);
            }

            if (environmentInfoResponse.status === 429) {
                throw new RateLimitingError(message);
            }

            if (environmentInfoResponse.status === 404) {
                throw new HttpError(404, 'No workspace found.');
            }

            throw new HttpError(environmentInfoResponse.status, environmentInfoResponse.statusText);
        }

        try {
            const responseJSON: IEnvironment = await environmentInfoResponse.json();
            return responseJSON;
        } catch (e) {
            throw new HttpError(500, e.message);
        }
    };

    public startCodespace = async (codespace: IEnvironment, token: string) => {
        // all write operations should go to the region the codespace is in
        const apiEndpoint = config.getCodespaceRegionalApiEndpoint(codespace);

        const url = new URL(`${apiEndpoint}/environments/${codespace.id}/start`);

        const headers = {
            Authorization: `Bearer ${token}`,
            [headerNames.acceptRedirects]: 'false',
        };

        let envStartResponse = await fetch(url.toString(), {
            method: 'POST',
            headers,
        });

        if (envStartResponse.status === 333) {
            const { location: redirectLocation } = await envStartResponse.json();
            if (redirectLocation) {
                envStartResponse = await fetch(redirectLocation, { method: 'POST', headers });
            }
        }

        if (!envStartResponse.ok) {
            throw new Error(`${envStartResponse.status} ${envStartResponse.statusText}`);
        }
    };
}

export const vsoAPI = new VsoAPI();
