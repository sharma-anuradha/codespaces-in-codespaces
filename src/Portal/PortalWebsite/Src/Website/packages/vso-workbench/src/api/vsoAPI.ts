import { IEnvironment, isHostedOnGithub } from 'vso-client-core';

import { config } from '../config/config';
import { AuthenticationError } from '../errors/AuthenticationError';
import { RateLimitingError } from '../errors/ReteLimitingError';
import { HttpError } from '../errors/HttpError';
import { authService } from '../auth/authService';
import { appendUrlPath } from '../utils/appendUrlPath';

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
                await authService.signOut();
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

    /**
     * Since GitHub needs to have control over the codespace permissions,
     * we need to call their API proxy so they can prevent the codespace
     * from being started for offboarded or blocked users.
     */
    public startCodespace = async (codespace: IEnvironment) => {
        const token = isHostedOnGithub()
            ? await authService.getCachedGithubToken()
            : await authService.getCachedToken();

        // VSCS API implements the "fake redirection" responses as a workaround
        // for the `Origin: null` header set by the browser due to "opaque response"
        // caused by cross-domain redirection during the CORS handshake.
        // The `fake redirection` represents a HTTP 333 response with `location`
        // inside the response body that the client uses for manual redirection.
        const headers = isHostedOnGithub()
            ? {}
            : { [headerNames.acceptRedirects]: 'false' };

        if (!token) {
            throw new AuthenticationError('Cannot find auth token.');
        }

        const endpoint = await config.getProxiedApiEndpoint(codespace);
        return await this.requestToStartCodespace(
            appendUrlPath(endpoint, `/environments/${codespace.id}/start`),
            token,
            headers
        );
    };

    private requestToStartCodespace = async (
        apiURL: string,
        token: string,
        additionalHeaders: Record<string, string> = {}
    ) => {
        const url = new URL(apiURL);

        const headers = {
            Authorization: `Bearer ${token}`,
            ...additionalHeaders,
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
