import { Context } from './useActionContext';
import {
    WithType,
    BaseActionWithContext,
    ActionWithPayloadWithContext,
    ErrorActionWithContext,
    AutoType,
    BaseAction,
    ActionWithPayload,
    ErrorAction,
} from './types';

export function useActionCreator() {
    // prettier-ignore
    function createContextAwareAction<T extends string>(actionType: T): WithType<BaseActionWithContext<T>>;
    // prettier-ignore
    function createContextAwareAction<T extends string, E extends Error>(actionType: T, payload: E): WithType<ErrorActionWithContext<T, undefined, E>>;
    // prettier-ignore
    function createContextAwareAction<T extends string, P extends {}>(actionType: T, payload: P): WithType<ActionWithPayloadWithContext<T, P>>;
    // prettier-ignore
    function createContextAwareAction<T extends string, P extends {}, E extends Error>(actionType: T, payload: P, error: E): WithType<ErrorActionWithContext<T, P, E>>;

    function createContextAwareAction<T extends string, P, E extends Error>(
        type: T,
        payload?: P,
        error?: E
    ) {
        // Having function name in stack traces here will be useful.
        const applyContextToAction =
            // tslint:disable-next-line: no-function-expression
            function applyContextToAction(context: Context) {
                let pureAction;

                if (error && payload) {
                    pureAction = action(type, payload, error);
                } else if (payload) {
                    pureAction = action(type, payload);
                } else if (error) {
                    pureAction = action(type, error);
                } else {
                    pureAction = action(type);
                }

                return {
                    ...pureAction,
                    metadata: {
                        correlationId: context.__id,
                    },
                };
            } as AutoType<T, P, E>;

        // For debugging purposes we'll put action type here as well.
        (applyContextToAction as WithType<AutoType<T, P, E>>).type = type;

        return applyContextToAction as WithType<AutoType<T, P, E>>;
    }

    return createContextAwareAction;
}

// prettier-ignore
export function action<T extends string>(actionType: T): BaseAction<T>;
// prettier-ignore
export function action<T extends string, E extends Error>(actionType: T, error: E): ErrorAction<T, undefined, E>;
// prettier-ignore
export function action<T extends string, P extends {}>(actionType: T, payload: P): ActionWithPayload<T, P>;
// prettier-ignore
export function action<T extends string, P extends {}, E extends Error>(actionType: T, payload: P, error: E): ErrorAction<T, P, E>;

export function action(type: any, payload?: any, error?: any) {
    if (payload != null && error != null) {
        return {
            type,
            payload,
            error,
            failed: true,
        };
    } else if (payload != null && payload instanceof Error) {
        return {
            type,
            error: payload,
            failed: true,
        };
    } else if (payload != null) {
        return {
            type,
            payload,
            failed: false,
        };
    } else if (error != null) {
        return {
            type,
            payload,
            error,
            failed: true,
        };
    } else {
        return {
            type,
            failed: false,
        };
    }
}
