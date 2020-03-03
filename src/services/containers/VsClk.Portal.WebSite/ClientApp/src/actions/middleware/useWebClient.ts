import { useActionContext } from './useActionContext';

import { trace as baseTrace } from '../../utils/trace';
import { getTopLevelDomain } from '../../utils/getTopLevelDomain';
import { wait } from '../../dependencies';
import { sendTelemetry } from '../../utils/telemetry';
import { isDefined } from '../../utils/isDefined';

const trace = baseTrace.extend('useWebClient:trace');
// tslint:disable-next-line: no-console
trace.log = console.trace.bind(console);

const requestIdHeader = 'vssaas-request-id';

const defaultRequestOptions = {
    requiresAuthentication: true,
    skipParsingResponse: false,
    retryCount: 0,
} as const;

type RequestOptions = {
    requiresAuthentication: boolean;
    skipParsingResponse: boolean;
    retryCount: number;
    shouldRetry?: (response: Response | undefined, retry: number) => boolean | Promise<boolean>;
};

async function request<TResult>(
    url: string,
    options: RequestInit,
    requestOptions?: undefined
): Promise<TResult>;
async function request<TResult>(
    url: string,
    options: RequestInit,
    requestOptions: Partial<Omit<RequestOptions, 'skipParsingResponse'>> & {
        skipParsingResponse: true;
    }
): Promise<Response>;
async function request<TResult>(
    url: string,
    options: RequestInit,
    requestOptions: Partial<Omit<RequestOptions, 'skipParsingResponse'>> & {
        skipParsingResponse?: false;
    }
): Promise<TResult>;
async function request<TResult>(
    url: string,
    options: RequestInit,
    requestOptions: Partial<RequestOptions>
): Promise<TResult | Response>;
// tslint:disable-next-line: max-func-body-length
async function request<TResult>(
    url: string,
    options: RequestInit,
    requestOptions: Partial<RequestOptions> = defaultRequestOptions
): Promise<TResult | Response> {
    // Since in tests we are creating the config manually, empty url can slip
    if (process.env.NODE_ENV === 'test' && !url) {
        trace('Calling service with empty url.');
        throw new Error('Missing URL');
    }

    const retryCount = requestOptions.retryCount || defaultRequestOptions.retryCount;
    return await requestInternal(0);

    // tslint:disable-next-line: max-func-body-length
    async function requestInternal(retry: number): Promise<TResult | Response> {
        requestOptions = {
            ...defaultRequestOptions,
            ...requestOptions,
        };

        let { headers, body, ...rest } = options;

        if (requestOptions.requiresAuthentication) {
            const context = useActionContext();

            const { token } = context.state.authentication;

            if (!token) {
                throw new ServiceAuthenticationError();
            }

            headers = {
                Authorization: `Bearer ${token}`,
                ...headers,
            } as Record<string, string>;
        }

        let response;
        try {
            headers = {
                'Content-Type': 'application/json',
                ...headers,
            } as Record<string, string>;

            const { makeRequest } = useActionContext();

            response = await makeRequest(url, {
                ...rest,
                credentials: 'same-origin',
                headers,
                body,
            });
        } catch (err) {
            if (requestOptions.shouldRetry && (await requestOptions.shouldRetry(response, retry))) {
                return requestInternal(retry + 1);
            }

            if (!requestOptions.shouldRetry && retry < retryCount) {
                await wait(retry * 1000);
                return requestInternal(retry + 1);
            }

            throw new ServiceConnectionError(err);
        }

        const responseRequestId = response.headers.get(requestIdHeader);
        if (isDefined(responseRequestId)) {
            sendTelemetry('vsonline/request', {
                requestId: responseRequestId,
            });
        }

        if (
            !response.ok &&
            requestOptions.shouldRetry &&
            (await requestOptions.shouldRetry(response, retry))
        ) {
            return requestInternal(retry + 1);
        }

        if (!requestOptions.shouldRetry && response.status === 500 && retry < retryCount) {
            await wait(retry * 1000);
            return requestInternal(retry + 1);
        }

        // if 307 from services, manually follow the redirect
        if (response.status === 307) {
            trace('Redirect: ', response);
            const redirectUrl = response.headers.get('location');
            if (redirectUrl) {
                // allow only vs domain redirects
                if (getTopLevelDomain(redirectUrl) === 'visualstudio.com') {
                    const opts: RequestInit = {
                        ...options,
                    };

                    const prevUrlOrigin = new URL(url);
                    const redirectUrlOrigin = new URL(redirectUrl);

                    if (prevUrlOrigin.origin !== redirectUrlOrigin.origin) {
                        opts.mode = 'cors';
                    }

                    trace('Follow redirect: ', redirectUrl, opts, requestOptions);
                    return await request(redirectUrl, opts, requestOptions);
                }
            }
        }

        if (response.status === 401) {
            throw new ServiceAuthenticationError();
        }

        if (!response.ok) {
            throw new ServiceResponseError(url, response.status, response);
        }

        if (requestOptions.skipParsingResponse) {
            return response;
        }

        try {
            const content = await response.json();
            return content as TResult;
        } catch (err) {
            throw new ServiceContentError(url, response.status);
        }
    }
}

async function getRequest<TResult = {}>(
    url: string,
    requestOptions: Partial<Omit<RequestOptions, 'skipParsingResponse'>> = {}
) {
    return await request<TResult>(
        url,
        {
            method: 'GET',
        },
        { ...requestOptions, skipParsingResponse: false }
    );
}

function isValidRequestBody(obj: RequestInit['body'] | {}): obj is RequestInit['body'] {
    if (!obj) {
        return true;
    }

    if (obj.toString() !== '[object Object]') {
        return true;
    }

    // tslint:disable-next-line: no-typeof-undefined
    if (typeof ReadableStream !== 'undefined' && obj instanceof ReadableStream) {
        return true;
    }

    // There's probably more cases that are not valid, but from the list
    // we are interested in, this will do.

    return false;
}

async function postRequest<TResult = object>(
    url: string,
    requestBody: RequestInit['body'] | {}
): Promise<TResult>;
async function postRequest<TResult = object>(
    url: string,
    requestBody: RequestInit['body'] | {},
    requestOptions?: Partial<RequestOptions>
): Promise<TResult>;
async function postRequest<TResult = object>(
    url: string,
    requestBody: RequestInit['body'] | {},
    requestOptions?: Partial<RequestOptions>
) {
    let body: RequestInit['body'];
    if (!isValidRequestBody(requestBody)) {
        body = JSON.stringify(requestBody);
    } else {
        body = requestBody;
    }

    if (!requestOptions) {
        return request(url, {
            method: 'POST',
            body,
        });
    }

    return await request<TResult>(
        url,
        {
            method: 'POST',
            body,
        },
        requestOptions
    );
}

async function putRequest<TResult = object>(
    url: string,
    requestBody: RequestInit['body'] | {},
    requestOptions?: Partial<RequestOptions>
) {
    let body: RequestInit['body'];
    if (!isValidRequestBody(requestBody)) {
        body = JSON.stringify(requestBody);
    } else {
        body = requestBody;
    }

    return await request<TResult>(
        url,
        {
            method: 'PUT',
            body,
        },
        requestOptions || defaultRequestOptions
    );
}

async function deleteRequest<TResult = void>(
    url: string,
    requestOptions: Partial<RequestOptions> = {}
) {
    return await request<TResult>(
        url,
        {
            method: 'DELETE',
        },
        { skipParsingResponse: true, ...requestOptions }
    );
}

async function patchRequest<TResult = object>(
    url: string,
    requestBody: RequestInit['body'] | {}
): Promise<TResult>;
async function patchRequest<TResult = object>(
    url: string,
    requestBody: RequestInit['body'] | {},
    requestOptions?: Partial<RequestOptions>
): Promise<TResult>;
async function patchRequest<TResult = object>(
    url: string,
    requestBody: RequestInit['body'] | {},
    requestOptions?: Partial<RequestOptions>
) {
    let body: RequestInit['body'];
    if (!isValidRequestBody(requestBody)) {
        body = JSON.stringify(requestBody);
    } else {
        body = requestBody;
    }

    if (!requestOptions) {
        return request(url, {
            method: 'PATCH',
            body,
        });
    }

    return await request<TResult>(
        url,
        {
            method: 'PATCH',
            body,
        },
        requestOptions
    );
}

// tslint:disable-next-line: max-func-body-length
export function useWebClient() {
    return {
        request,
        get: getRequest,
        post: postRequest,
        put: putRequest,
        delete: deleteRequest,
        patch: patchRequest,
    };
}

export class ServiceError extends Error {
    constructor(message: string) {
        super(message);

        if (typeof Error.captureStackTrace === 'function') {
            Error.captureStackTrace(this, ServiceError);
        }
    }
}

export class ServiceResponseError extends ServiceError {
    constructor(
        public readonly url: string,
        public readonly statusCode: number,
        public readonly response: Response
    ) {
        super('Service request failed');

        if (typeof Error.captureStackTrace === 'function') {
            Error.captureStackTrace(this, ServiceResponseError);
        }
    }
}

export class ServiceContentError extends ServiceError {
    constructor(public url: string, public statusCode: number) {
        super('Service content not valid JSON.');

        if (typeof Error.captureStackTrace === 'function') {
            Error.captureStackTrace(this, ServiceContentError);
        }
    }
}

export class ServiceConnectionError extends ServiceError {
    constructor(public error: Error) {
        super('Service connection failed. ' + error.message);

        if (typeof Error.captureStackTrace === 'function') {
            Error.captureStackTrace(this, ServiceConnectionError);
        }
    }
}

export class ServiceAuthenticationError extends ServiceError {
    constructor() {
        super('Authentication Failed.');

        if (typeof Error.captureStackTrace === 'function') {
            Error.captureStackTrace(this, ServiceAuthenticationError);
        }
    }
}
