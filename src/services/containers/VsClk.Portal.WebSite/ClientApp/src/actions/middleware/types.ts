import { Context } from './useActionContext';
import { AnyAction } from 'redux';

export interface BaseAction<
    TType extends string = string,
    TPayload = unknown,
    TCustomError = unknown
> {
    type: TType;
    payload?: TPayload;
    failed: boolean;
    error?: TCustomError;
}

export interface ActionWithPayload<
    TType extends string = string,
    TPayload = unknown,
    TCustomError = undefined
> extends BaseAction<TType, TPayload, TCustomError> {
    payload: TPayload;
}

export interface ErrorAction<
    TType extends string = string,
    TPayload = unknown,
    TCustomError extends Error = Error
> extends BaseAction<TType, TPayload, TCustomError> {
    error: TCustomError;
    failed: true;
}

export type WithMetadata<T> = T & {
    metadata: {
        correlationId: string;
    };
};

export type WithType<T> = T & { type: string };

export interface BaseActionWithContext<T extends string = string> {
    (context: Context): WithMetadata<BaseAction<T>>;
}

export interface ActionWithPayloadWithContext<T extends string = string, P = undefined> {
    (context: Context): WithMetadata<ActionWithPayload<T, P>>;
}

export interface ErrorActionWithContext<
    T extends string = string,
    P = undefined,
    E extends Error = Error
> {
    (context: Context): WithMetadata<ErrorAction<T, P, E>>;
}

export type AutoType<T extends string, P, E> = E extends Error
    ? ErrorActionWithContext<T, P, E>
    : P extends undefined
    ? BaseActionWithContext<T>
    : ActionWithPayloadWithContext<T, P>;

type UnwrapDispatch<T> = T extends (dispatch: Dispatch) => Promise<infer U> ? Promise<U> : never;

export interface Dispatch {
    <T extends string, P, E extends Error>(action: ErrorActionWithContext<T, P, E>): WithMetadata<
        ErrorAction<T, P, E>
    >;
    <T extends string, P>(action: ActionWithPayloadWithContext<T, P>): WithMetadata<
        ActionWithPayload<T, P>
    >;
    <T extends string>(action: BaseActionWithContext<T>): WithMetadata<BaseAction<T>>;

    <T extends (dispatch: Dispatch) => Promise<any>>(param: T): UnwrapDispatch<T>;
    <T>(param: T): T;
    (param: undefined): undefined;
}

export interface DispatchWithContext {
    <T extends string, P, E extends Error>(
        action: ErrorActionWithContext<T, P, E>,
        context: Context
    ): WithMetadata<ErrorAction<T, P, E>>;
    <T extends string, P>(
        action: ActionWithPayloadWithContext<T, P>,
        context: Context
    ): WithMetadata<ActionWithPayload<T, P>>;
    <T extends string>(action: BaseActionWithContext<T>, context: Context): WithMetadata<
        BaseAction<T>
    >;

    <T>(action: Promise<T>): Promise<T>;
    (action: Promise<void>): Promise<void>;
    (action: void, context: Context): void;
    (action: AnyAction): any;
    (action: void): void;
}
