import { Middleware } from 'redux';
import { recreateContext, Context } from './useActionContext';
import { unhandledAsyncError } from '../unhandledAsyncError';
import { isThenable } from '../../utils/isThenable';
import { createTrace } from '../../utils/createTrace';
import { DispatchWithContext } from './types';
import { ApplicationState } from '../../reducers/rootReducer';

const trace = createTrace('middleware');

// tslint:disable-next-line: export-name
export type ActionContextMiddleware = Middleware<
    DispatchWithContext,
    ApplicationState,
    DispatchWithContext
>;

// tslint:disable-next-line: max-func-body-length
export const actionContextMiddleware: ActionContextMiddleware = (api) => (next) => (
    ...params: any[]
) => {
    const [action] = params;
    const context = (params[1] as Context | undefined) || recreateContext();

    if (action == null) {
        trace.verbose('probably nothing to do. (this most likely should not happen?)');
        return;
    }

    // 1. Action can be a promise
    if (isThenable(action)) {
        trace.verbose('executeAction: handling promise');

        let newPromise = action.then((action) => {
            trace.verbose('executeAction: calling dispatch with action', action);
            if (!action) {
                return undefined;
            }

            // Run the result through the middleware again
            return api.dispatch(action, context);
        });

        if (!context.shouldThrowFailedActionsAsErrors) {
            newPromise = newPromise.catch((err) => {
                trace.verbose('executeAction: action failed', action);

                api.dispatch(unhandledAsyncError(err), context);

                if (process.env.NODE_ENV === 'development') {
                    // Console has special handling in dev mode where we want to see this as much as possible.
                    // tslint:disable-next-line: no-console
                    console.error(err);
                }

                return undefined;
            });
        }

        return newPromise;
    }

    // 2. Action can be a function that applies context to action content (e.g. correlation id, performance scenario steps, ...)
    if (typeof action === 'function') {
        trace.verbose('executeAction: handling context aware action');

        return api.dispatch(action(context), context);
    }

    // Arbitrary values can be dispatched, we only want to further process actions with type
    if (action.type === undefined) {
        return action;
    }

    trace.verbose('executeAction: executing simple action', action);
    return next(action);
};
