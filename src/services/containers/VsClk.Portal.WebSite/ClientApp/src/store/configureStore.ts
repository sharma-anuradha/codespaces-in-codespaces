import { createStore, applyMiddleware, Middleware } from 'redux';
import thunk, { ThunkMiddleware } from 'redux-thunk';

import { rootReducer, ApplicationState } from '../reducers/rootReducer';
import { trace } from '../utils/trace';

const logger = (_store: any) => (next: Function) => (action: { type: string; error?: Error }) => {
    trace(`dispatching ${action.type}`);

    if (action.error) {
        trace(action.error);
    }

    return next(action);
};

const middleware: Middleware[] = [];
middleware.push(thunk as ThunkMiddleware<ApplicationState>);
if (process.env.NODE_ENV === 'development') {
    middleware.push(logger);
}

export function configureStore(preloadedState?: Partial<ApplicationState>) {
    return createStore(rootReducer, preloadedState, applyMiddleware(...middleware));
}
