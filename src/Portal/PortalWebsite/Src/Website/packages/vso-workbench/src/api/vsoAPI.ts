import { IEnvironment } from 'vso-client-core';

import { config } from '../config/config';
import { AuthenticationError } from '../errors/AuthenticationError';
import { RateLimitingError } from '../errors/ReteLimitingError';
import { HttpError } from '../errors/HttpError';

const cache: { [key: string]: Promise<IEnvironment> | undefined } = {};

const headerNames = {
    acceptRedirects: 'X-Can-Accept-Redirects',
};

function isFakeRedirectResponse(respose: Response) {
    // Browsers won't allow CORS redirects so we use custom status code 333
    // to do a "fake" redirect from our services.
    return respose.status === 333;
}

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

        if (isFakeRedirectResponse(environmentInfoResponse)) {
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
        const options = {
            method: 'POST',
            headers,
        };

        let envStartResponse = await fetch(url.toString(), options);

        if (isFakeRedirectResponse(envStartResponse)) {
            const { location: redirectLocation } = await envStartResponse.json();
            if (redirectLocation) {
                envStartResponse = await fetch(redirectLocation, options);
            }
        }

        if (!envStartResponse.ok) {
            throw new HttpError(envStartResponse.status, envStartResponse.statusText);
        }
    };

    public suspendCodespace = async (codespace: IEnvironment, token: string) => {
        // all write operations should go to the region the codespace is in
        const apiEndpoint = config.getCodespaceRegionalApiEndpoint(codespace);

        const url = new URL(`${apiEndpoint}/environments/${codespace.id}/shutdown`);

        const headers = {
            Authorization: `Bearer ${token}`,
            [headerNames.acceptRedirects]: 'false',
        };
        const options = {
            method: 'POST',
            headers,
        };

        let response = await fetch(url.toString(), options);

        if (isFakeRedirectResponse(response)) {
            const { location: redirectLocation } = await response.json();
            if (redirectLocation) {
                response = await fetch(redirectLocation, options);
            }
        }

        if (!response.ok) {
            throw new HttpError(response.status, response.statusText);
        }
    };
}

export const vsoAPI = new VsoAPI();
