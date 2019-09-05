import { createUniqueId } from '../../dependencies';
import { DispatchWithContext } from './types';
import { ApplicationState } from '../../reducers/rootReducer';
import { MiddlewareAPI } from 'redux';

let context: Context | undefined;

export class Context {
    private makeRequestFunc: typeof fetch = fetch;
    private test_stateOverride: Partial<ApplicationState> = {};

    public storeApi?: MiddlewareAPI<DispatchWithContext, ApplicationState>;
    public shouldThrowFailedActionsAsErrors = false;

    readonly __id: string;
    readonly __instanceId = createUniqueId();

    constructor(id?: string) {
        this.__id = id || createUniqueId();
    }

    get makeRequest() {
        return this.makeRequestFunc;
    }

    test_setMockRequestFactory = (makeRequestFunc: typeof fetch) => {
        this.makeRequestFunc = makeRequestFunc;
    };

    get state() {
        if (!this.storeApi) {
            throw new Error('InfraError: State is not available yet to actions');
        }

        if (process.env.NODE_ENV === 'test') {
            return {
                ...this.storeApi.getState(),
                ...this.test_stateOverride,
            };
        }

        return this.storeApi!.getState();
    }

    test_setApplicationState(state: Partial<ApplicationState>) {
        if (process.env.NODE_ENV !== 'test') {
            throw new Error('Cannot use test methods outside of tests');
        }

        this.test_stateOverride = state;
    }

    dispatch(action: any) {
        if (!this.storeApi) {
            throw new Error('InfraError: Store API is not available yet to actions');
        }

        return this.storeApi!.dispatch(action, this);
    }

    clone() {
        const clone = new Context(this.__id);

        clone.makeRequestFunc = this.makeRequestFunc;
        clone.test_stateOverride = this.test_stateOverride;
        clone.storeApi = this.storeApi;

        return clone;
    }
}

type ContextFactory = () => Context;
let contextFactory: ContextFactory = () => new Context();
export function setContextFactory(factory: ContextFactory) {
    if (process.env.NODE_ENV === 'test' && context) {
        context.test_setApplicationState({});
    }

    contextFactory = factory;

    recreateContext();
}

function createContext() {
    return contextFactory();
}

export function useActionContext() {
    if (!context) {
        context = createContext();
    }

    return context;
}

export function recreateContext() {
    if (process.env.NODE_ENV === 'test' && context) {
        const makeRequest = context.makeRequest;
        const stateOverride = (context as any).test_stateOverride;
        context = createContext();
        context.test_setMockRequestFactory(makeRequest);
        context.test_setApplicationState(stateOverride);

        return context;
    }

    context = createContext();

    return context;
}
