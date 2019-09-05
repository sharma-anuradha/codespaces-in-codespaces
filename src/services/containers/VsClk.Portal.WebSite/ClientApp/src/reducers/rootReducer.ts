import { combineReducers } from 'redux';

import { authentication } from './authentication';
import { configuration } from './configuration';
import { environments } from './environments';
import { userInfo } from './userInfo';
import { testReducer as __test } from './testReducer';

import { BaseAction } from '../actions/middleware/types';

type Reducers = {
    [name: string]: (...params: any[]) => any;
};
type ReducersToState<T extends Reducers> = { [k in keyof T]: ReturnType<T[k]> };

const reducers = { authentication, configuration, environments, userInfo, __test };

type ReducerAcceptedAction<
    T extends (state: ApplicationState, action: any) => ApplicationState
> = Parameters<T>[1];
type ReducersToAcceptedActions<T extends Reducers> = ReducerAcceptedAction<T[keyof T]>;

type State<T extends (state: any, action: any) => any> = T extends (
    state: infer S | undefined,
    action: any
) => any
    ? S
    : never;

type BaseActionAcceptingReducer<T extends Reducers> = {
    [k in keyof T]: (state: State<T[k]> | undefined, action: BaseAction) => State<T[k]>;
};

export type ApplicationState = ReducersToState<typeof reducers>;
export const rootReducer = combineReducers(reducers as BaseActionAcceptingReducer<typeof reducers>);
