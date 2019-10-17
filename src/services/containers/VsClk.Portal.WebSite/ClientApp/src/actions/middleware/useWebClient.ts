import { useActionContext } from './useActionContext';

import { trace as baseTrace } from '../../utils/trace';
import { getTopLevelDomain } from '../../utils/getTopLevelDomain';

const trace = baseTrace.extend('useWebClient:trace');
// tslint:disable-next-line: no-console
trace.log = console.trace.bind(console);

const defaultRequestOptions = {
    requiresAuthentication: true,
    skipParsingResponse: false,
} as const;

type RequestOptions = {
    requiresAuthentication: boolean;
    skipParsingResponse: boolean;
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

    requestOptions = {
        ...defaultRequestOptions,
        ...requestOptions,
    };

    let { headers, body, ...rest } = options;

    const {
        state: {
            authentication: { token },
        },
    } = useActionContext();

    if (requestOptions.requiresAuthentication && !token) {
        throw new ServiceAuthenticationError();
    }

    let response;
    try {
        headers = {
            'Content-Type': 'application/json',
            ...headers,
        } as Record<string, string>;

        if (requestOptions.requiresAuthentication && token) {
            headers = {
                Authorization: `Bearer ${token!.accessToken}`,
                ...headers,
            } as Record<string, string>;
        }

        const { makeRequest } = useActionContext();

        response = await makeRequest(url, {
            ...rest,
            credentials: 'same-origin',
            headers,
            body,
        });
    } catch (err) {
        throw new ServiceConnectionError(err);
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
        throw new ServiceResponseError(url, response.status);
    }

    if (requestOptions.skipParsingResponse) {
        return response;
    }

    try {
        const content = await response.json();
        console.log(response);
        console.log(content);

        return content as TResult;
    } catch (err) {
        throw new ServiceContentError(url, response.status);
    }
}

async function getRequest<TResult = {}>(url: string) {
    return await request<TResult>(url, {
        method: 'GET',
    });
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

async function putRequest<TResult = object>(url: string, requestBody: RequestInit['body'] | {}) {
    let body: RequestInit['body'];
    if (!isValidRequestBody(requestBody)) {
        body = JSON.stringify(requestBody);
    } else {
        body = requestBody;
    }

    return await request<TResult>(url, {
        method: 'PUT',
        body,
    });
}

async function deleteRequest<TResult = void>(url: string) {
    return await request<TResult>(
        url,
        {
            method: 'DELETE',
        },
        { skipParsingResponse: true }
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
    };
}

export class ServiceError extends Error {
    constructor(message: string) {
        super(message);
        Error.captureStackTrace(this, ServiceError);
    }
}

export class ServiceResponseError extends ServiceError {
    constructor(public url: string, public statusCode: number) {
        super('Service request failed');
        Error.captureStackTrace(this, ServiceResponseError);
    }
}

export class ServiceContentError extends ServiceError {
    constructor(public url: string, public statusCode: number) {
        super('Service content not valid JSON.');
        Error.captureStackTrace(this, ServiceContentError);
    }
}

export class ServiceConnectionError extends ServiceError {
    constructor(public error: Error) {
        super('Service connection failed');

        Error.captureStackTrace(this, ServiceConnectionError);
    }
}

export class ServiceAuthenticationError extends ServiceError {
    constructor() {
        super('Authentication Failed.');
        Error.captureStackTrace(this, ServiceAuthenticationError);
    }
}
