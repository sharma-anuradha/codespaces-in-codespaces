import { combineReducers } from 'redux';

import { authentication } from './authentication';
import { configuration } from './configuration';
import { environments } from './environments';

type Reducers = {
    [name: string]: (...params: any[]) => any;
};
type ReducersToState<T extends Reducers> = { [k in keyof T]: ReturnType<T[k]> };

const reducers = { authentication, configuration, environments };

export type ApplicationState = ReducersToState<typeof reducers>;
export const rootReducer = combineReducers(reducers);
