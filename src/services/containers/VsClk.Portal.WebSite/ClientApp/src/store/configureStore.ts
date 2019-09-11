import { createStore, applyMiddleware } from 'redux';
import { rootReducer, ApplicationState } from '../reducers/rootReducer';
import { trace } from '../utils/trace';
import { setContextFactory, Context } from '../actions/middleware/useActionContext';
import { actionContextMiddleware } from '../actions/middleware/middleware';
import { DispatchError } from '../actions/middleware/useDispatch';
import { BaseAction, ErrorAction, DispatchWithContext, WithMetadata } from '../actions/middleware/types';
import { telemetry, IActionTelemetryProperties } from '../utils/telemetry';

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

const actionTelemetry = (store: { getState(): ApplicationState }) => (next: Function) => (action: WithMetadata< BaseAction | ErrorAction>) => {
    let actionName = action.type;
    const token = store.getState().authentication.token;

    const eventProperties: IActionTelemetryProperties = {
        action: actionName,
        correlationId: action.metadata.correlationId,
        isInternal: (token && token.account.userName.includes('@microsoft.com'))
                    ? true
                    : false
    }

    if (action.failed) {
        telemetry.trackErrorAction(eventProperties, {})
    } else {
        telemetry.trackSuccessAction(eventProperties, {});
    }

    return next(action);
};

let middleware = [actionContextMiddleware];

if (process.env.NODE_ENV === 'development') {
    middleware.push(logger);
}

if (process.env.NODE_ENV !== 'test') {
    middleware.push(actionTelemetry)
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
