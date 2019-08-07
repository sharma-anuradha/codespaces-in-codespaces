interface BaseAction<
    TType extends string = string,
    TPayload = undefined,
    TCustomError = undefined
> {
    type: TType;
    payload?: TPayload;
    failed: boolean;
    error?: TCustomError;
}

interface ActionWithPayload<
    TType extends string = string,
    TPayload = undefined,
    TCustomError = undefined
> extends BaseAction<TType, TPayload, TCustomError> {
    payload: TPayload;
}

interface ErrorAction<
    TType extends string = string,
    TPayload = undefined,
    TCustomError extends Error = Error
> extends BaseAction<TType, TPayload, TCustomError> {
    error: TCustomError;
    failed: true;
}

export interface Dispatch {
    <T>(param: T): T;
}

// prettier-ignore
export function action<T extends string>(actionType: T): BaseAction<T>;
// prettier-ignore
export function action<T extends string, P>(actionType: T, payload: P): ActionWithPayload<T, P>;
// prettier-ignore
export function action<T extends string, P, E extends Error>(actionType: T, payload: P, error: E): ErrorAction<T, P, E>;

export function action(type: string, payload?: any, error?: any) {
    if (payload != null && error != null) {
        return {
            type,
            payload,
            error,
            failed: true,
        };
    } else if (payload != null) {
        return {
            type,
            payload,
            failed: false,
        };
    } else {
        return {
            type,
            failed: false,
        };
    }
}
