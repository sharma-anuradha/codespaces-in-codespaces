import * as msal from '@vs/msal';

import { wait } from '../dependencies';
import { ApplicationState } from '../reducers/rootReducer';
import { BaseAction, ErrorAction, WithMetadata } from '../actions/middleware/types';
import {
    setContextFactory,
    Context,
    useActionContext,
} from '../actions/middleware/useActionContext';
import { configureStore } from '../store/configureStore';

import {
    createEnvironmentActionType,
    CreateEnvironmentAction,
    createEnvironmentSuccessActionType,
    CreateEnvironmentSuccessAction,
    createEnvironmentFailureActionType,
    CreateEnvironmentFailureAction,
} from '../actions/createEnvironment';

jest.mock('./telemetry', () => ({
    sendTelemetry: jest.fn(),
    telemetry: {
        initializeTelemetry: jest.fn()
    },
}));

jest.setTimeout(1000);

jest.spyOn(window, 'fetch').mockImplementation(() => {
    throw new Error('network requests not allowed in test');
});

declare global {
    namespace jest {
        interface Matchers<R> {
            toBeHaveBeenDispatched(actionType: string): R;
            toHaveFailed(): R;
            toHaveBeenDispatchedInOrder(...actionTypes: string[]): R;
        }
    }
}

export const testMsalToken = {
    accessToken: 'token',
    account: ({
        name: 'test',
        userName: 'test',
        idTokenClaims: { email: 'test@test.com', preferred_username: 'test' },
    } as unknown) as msal.Account,
    expiresOn: undefined!,
};

// prettier-ignore
export function getDispatchedAction(dispatchedActions: WithMetadata<BaseAction>[], actionType: typeof createEnvironmentActionType): WithMetadata<CreateEnvironmentAction>;
// prettier-ignore
export function getDispatchedAction(dispatchedActions: WithMetadata<BaseAction>[], actionType: typeof createEnvironmentFailureActionType): WithMetadata<CreateEnvironmentFailureAction>;
// prettier-ignore
export function getDispatchedAction(dispatchedActions: WithMetadata<BaseAction>[], actionType: typeof createEnvironmentSuccessActionType): WithMetadata<CreateEnvironmentSuccessAction>;
// prettier-ignore
export function getDispatchedAction(dispatchedActions: WithMetadata<BaseAction>[], actionType: string): WithMetadata<BaseAction>;
// prettier-ignore
export function getDispatchedAction(dispatchedActions: BaseAction[], actionType: string): BaseAction {
    if (process.env.NODE_ENV !== 'test') {
        throw new Error('Test use only');
    }

    // As this is test utils, for convenience we assume the action will be there.
    // If it's not, then we want to fail the test anyway;
    return dispatchedActions.find((a) => a.type === actionType)!;
}

export function getFailedActions(dispatchedActions: (BaseAction | ErrorAction)[]): ErrorAction[] {
    function didActionFail(action: BaseAction | ErrorAction): action is ErrorAction {
        return action.failed;
    }
    const failedActions = dispatchedActions.filter(didActionFail);

    return failedActions;
}

expect.extend({
    toBeHaveBeenDispatched(dispatchedActions: WithMetadata<BaseAction>[], actionType: string) {
        const action = getDispatchedAction(dispatchedActions, actionType);
        if (action) {
            return {
                pass: true,
                message: () =>
                    `expected action "${actionType}" to not have been dispatched` +
                    '\n\n' +
                    `dispatched actions:\n${dispatchedActions.map((a) => a.type).join('\n')}`,
            };
        }

        return {
            pass: false,
            message: () =>
                `expected action "${actionType}" to have been dispatched` +
                '\n\n' +
                `dispatched actions:\n${dispatchedActions.map((a) => a.type).join('\n')}`,
        };
    },
    toHaveFailed(dispatchedActions: BaseAction[]) {
        const actions = getFailedActions(dispatchedActions);
        if (actions.length) {
            const message = () => {
                return (
                    'Failed actions\n' +
                    actions
                        .map((action) => {
                            return action.type + ':\n' + action.error.stack;
                        })
                        .join('\n\n') +
                    `\nall dispatched actions:\n${dispatchedActions.map((a) => a.type).join('\n')}`
                );
            };
            return {
                pass: true,
                message,
            };
        }

        return {
            pass: false,
            message: () =>
                `expected action to fail` +
                '\n\n' +
                `dispatched actions:\n${dispatchedActions.map((a) => a.type).join('\n')}`,
        };
    },
    toHaveBeenDispatchedInOrder(dispatchedActions: BaseAction[], ...actionTypes: string[]) {
        const dispatchedActionTypes = dispatchedActions.map((a) => a.type);

        try {
            expect(dispatchedActionTypes).toEqual(actionTypes);
        } catch (err) {
            return err.matcherResult;
        }

        try {
            expect(dispatchedActionTypes).not.toEqual(actionTypes);
        } catch (err) {
            return err.matcherResult;
        }
    },
});

export type MockMakeRequestOptions = {
    shouldFailConnection?: boolean;
    delay?: number;
    readonly url?: string;

    responses?: {
        readonly headers?: Headers;
        readonly ok?: boolean;
        readonly redirected?: boolean;
        readonly status?: number;
        readonly statusText?: string;
        readonly shouldFailConnection?: boolean;
        readonly trailer?: Promise<Headers>;
        readonly type?: ResponseType;
        readonly body?: string | object | null | undefined;
    }[];
};

export const authenticated = {
    token: 'token',
    user: {
        email: 'test@test.test',
        name: 'test',
        username: 'test'
    },
    isAuthenticated: true,
    isAuthenticating: false,
    isInteractionRequired: false,
    isInternal: true
};

export function createMockMakeRequestFactory(options: MockMakeRequestOptions = {}): typeof fetch {
    const responseDefaults = {
        headers: new Headers({}),
        ok: true,
        redirected: false,
        status: 200,
        statusText: 'Mock All Good',
        trailer: Promise.resolve(new Headers({})),
        type: 'default' as ResponseType,
        shouldFailConnection: false,
    };
    const { delay = 0, responses = [] } = options;

    const fetchMock: typeof fetch = async (
        requestOptions: RequestInfo,
        _requestInit?: RequestInit
    ) => {
        let url = '';
        if (typeof requestOptions === 'string') {
            url = requestOptions;
        }
        await wait(delay);

        let {
            body = undefined,
            ok = responseDefaults.ok,
            status = responseDefaults.status,
            shouldFailConnection = responseDefaults.shouldFailConnection ||
            options.shouldFailConnection === true,
            ...rest
        } = responses.shift() || {};

        if (shouldFailConnection) {
            await wait(delay);
            throw new Error('MockFailedConnection');
        }

        const response: Response = {
            ...responseDefaults,
            ...rest,
            status,
            url,
            get ok() {
                return !!ok && (status < 400 && status >= 200);
            },
            clone() {
                throw Error('MockResponse.clone not implemented');
            },
            body: null,
            get bodyUsed() {
                return !!body;
            },
            async text() {
                await wait(delay);
                if (!body) {
                    throw new Error('MockResponse.body empty');
                }
                if (typeof body === 'string') {
                    return body;
                }
                return JSON.stringify(body);
            },
            async json() {
                await wait(delay);

                if (!body) {
                    throw new Error('MockResponse.body empty');
                }
                if (typeof body === 'string') {
                    return JSON.parse(body);
                }
                return body;
            },
            async arrayBuffer(): Promise<ArrayBuffer> {
                await wait(delay);
                throw new Error('MockResponse.arrayBuffer not implemented');
            },
            async blob(): Promise<Blob> {
                await wait(delay);
                if (body && body instanceof Blob) {
                    return body;
                }

                throw new Error('MockResponse.blob not implemented');
            },
            async formData(): Promise<FormData> {
                await wait(delay);
                throw new Error('MockResponse.formData not implemented');
            },
        };
        return response;
    };
    return fetchMock;
}

export type MockStore = ReturnType<typeof configureStore> & {
    dispatchedActions: WithMetadata<BaseAction>[];
};

export function createMockStore(preloadedState?: Partial<ApplicationState>): MockStore {
    const store = configureStore(preloadedState);

    setContextFactory(() => {
        const context = new Context();

        // Set initial state for the context, after this,
        // state will be updated by the middleware.
        context.storeApi = {
            getState: store.getState.bind(store),
            dispatch: store.dispatch.bind(store),
        };
        return context;
    });

    return new Proxy(store, {
        get(target: typeof store, property: keyof typeof store | 'dispatchedActions') {
            if (property === 'dispatchedActions') {
                return target.getState().__test.dispatchedActions;
            }

            return target[property];
        },
    }) as MockStore;
}

export function test_setMockRequestFactory(factory: typeof fetch) {
    const context = useActionContext();

    context.test_setMockRequestFactory(factory);
}

export function test_setApplicationState(state: Partial<ApplicationState>) {
    const context = useActionContext();

    context.test_setApplicationState(state);
}

export type Mutable<T> = { -readonly [P in keyof T]: T[P] };
