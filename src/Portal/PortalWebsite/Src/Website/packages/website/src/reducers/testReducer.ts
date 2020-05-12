import { BaseAction } from '../actions/middleware/types';

type TestState = {
    dispatchedActions: BaseAction[];
};

// This is supposed to throw when read.
const empty = null! as TestState;

const defaultState: TestState = {
    dispatchedActions: [],
};

export function testReducer(state: TestState = defaultState, action: BaseAction): TestState {
    if (process.env.NODE_ENV !== 'test') {
        return empty;
    }

    if (action.type === 'throw.reducer.failure') {
        throw new Error('throw.reducer.failure');
    }

    if (action.type && typeof action.type === 'string' && action.type.startsWith('@')) {
        return state;
    }

    return {
        dispatchedActions: [...state.dispatchedActions, action],
    };
}
