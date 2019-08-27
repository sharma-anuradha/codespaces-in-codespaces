import { useActionContext } from './useActionContext';
import {
    BaseAction,
    ActionWithPayload,
    ErrorAction,
    BaseActionWithContext,
    ErrorActionWithContext,
    ActionWithPayloadWithContext,
} from './types';
import { isThenable } from '../../utils/isThenable';

export type Dispatch = ReturnType<typeof useDispatch>;

export function useDispatch() {
    const context = useActionContext();

    const shouldThrow = context.shouldThrowFailedActionsAsErrors;
    context.shouldThrowFailedActionsAsErrors = true;

    type Action =
        | BaseAction
        | BaseActionWithContext
        | ActionWithPayload
        | ActionWithPayloadWithContext
        | ErrorAction
        | ErrorActionWithContext
        | Promise<unknown>
        | undefined;

    function customDispatch(action: void): void;
    function customDispatch<T extends Action>(action: T): T;
    function customDispatch<T extends Action>(action: T) {
        if (!action) {
            return;
        }

        const result = context.dispatch(action);

        if (isThenable(result)) {
            return result;
        }

        if (shouldThrow && result.failed) {
            throw new DispatchError(result.error);
        }

        return result;
    }
    return customDispatch;
}
export class DispatchError extends Error {
    constructor(public error: Error) {
        super('DispatchError');

        Error.captureStackTrace(this, DispatchError);
    }
}
