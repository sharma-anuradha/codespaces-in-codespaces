import { IEnvironment } from 'vso-client-core';

import { config } from '../config/config';
import { AuthenticationError } from '../errors/AuthenticationError';
import { RateLimitingError } from '../errors/ReteLimitingError';
import { HttpError } from '../errors/HttpError';

const cache: { [key: string]: Promise<IEnvironment> | undefined } = {};

export class VsoAPI {
    public getEnvironmentInfo = async (id: string, token: string) => {
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

        const environmentInfoResponse = await fetch(url, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        if (!environmentInfoResponse.ok) {
            const message = 'Cannot fetch environment info';

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

    public startEnvironment = async (id: string, token: string) => {
        const url = `${config.api}/environments/${id}/start`;

        const envStartResponse = await fetch(url, {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        if (!envStartResponse.ok) {
            throw new Error(`${envStartResponse.status} ${envStartResponse.statusText}`);
        }
    };
}

export const vsoAPI = new VsoAPI();
