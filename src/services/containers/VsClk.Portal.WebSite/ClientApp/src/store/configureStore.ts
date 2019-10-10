import { createStore, applyMiddleware } from 'redux';
import { rootReducer, ApplicationState } from '../reducers/rootReducer';
import { trace } from '../utils/trace';
import { setContextFactory, Context } from '../actions/middleware/useActionContext';
import { actionContextMiddleware } from '../actions/middleware/middleware';
import { DispatchError } from '../actions/middleware/useDispatch';
import {
    BaseAction,
    ErrorAction,
    DispatchWithContext,
    WithMetadata,
} from '../actions/middleware/types';
import { telemetry } from '../utils/telemetry';
import { ActionSuccessEvent } from '../utils/telemetry/ActionSuccessEvent';
import { ActionErrorEvent } from '../utils/telemetry/ActionErrorEvent';

const logger = (_store: unknown) => (next: Function) => (action: BaseAction | ErrorAction) => {
    trace(`dispatching ${action.type}`);

    if (action.error) {
        if (action.error instanceof DispatchError) {
            trace(action.error.error);
        }
        trace(action.error);
    }

    return next(action);
};

const actionTelemetry = (_: { getState(): ApplicationState }) => (next: Function) => (
    action: WithMetadata<BaseAction | ErrorAction>
) => {
    if (isErrorAction(action)) {
        telemetry.track(new ActionErrorEvent(action));
    } else {
        telemetry.track(new ActionSuccessEvent(action));
    }

    return next(action);
};

function isErrorAction(
    action: WithMetadata<BaseAction | ErrorAction>
): action is WithMetadata<ErrorAction> {
    return action.failed;
}

let middleware = [actionContextMiddleware];

if (process.env.NODE_ENV === 'development') {
    middleware.push(logger);
}

if (process.env.NODE_ENV !== 'test') {
    middleware.push(actionTelemetry);
}

export function configureStore(preloadedState?: Partial<ApplicationState>) {
    const store = createStore(rootReducer, preloadedState, applyMiddleware(...middleware));
    const getState = store.getState.bind(store);
    const dispatch = store.dispatch.bind(store);

    setContextFactory(() => {
        const context = new Context();

        // Set initial state for the context, after this,
        // state will be updated by the middleware.
        context.storeApi = {
            getState,
            dispatch,
        };

        return context;
    });

    // Ensure our type extension for dispatch result to precede defaults
    type EnhancedStoreType = { dispatch: DispatchWithContext } & typeof store;

    return store as EnhancedStoreType;
}
