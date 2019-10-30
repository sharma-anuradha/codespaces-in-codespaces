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
import { createMetadataFor } from './useActionCreator';

export type Dispatch = ReturnType<typeof useDispatch>;

export function useDispatch() {
    const context = useActionContext();

    type Action =
        | BaseAction
        | ActionWithPayload
        | ErrorAction
        | BaseActionWithContext
        | ActionWithPayloadWithContext
        | ErrorActionWithContext
        | Promise<unknown>
        | undefined;

    function customDispatch(action: void): void;
    function customDispatch<T extends ErrorAction>(action: T): never;
    function customDispatch<T extends Action>(action: T): T;
    function customDispatch<T extends Action>(action: T) {
        if (!action) {
            return;
        }

        if (typeof action !== 'function' && !isThenable(action)) {
            action = {
                ...action,
                metadata: createMetadataFor(action, context),
            };
        }

        const result = context.dispatch(action);

        if (isThenable(result)) {
            return result;
        }

        if (result.failed) {
            throw result.error;
        }

        return result;
    }
    return customDispatch;
}
