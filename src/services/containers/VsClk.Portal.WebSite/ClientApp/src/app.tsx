import React, { Component } from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';
import { connect, Provider } from 'react-redux';
import './app.css';

import { Main } from './components/main/main';
import { Welcome } from './components/welcome/welcome';
import { Workbench } from './components/workbench/workbench';

import { configureStore } from './store/configureStore';
import { init } from './actions/init';
import { AnyAction } from 'redux';
import { ThunkDispatch } from 'redux-thunk';
import { ApplicationState } from './reducers/rootReducer';
import { ProtectedRoute } from './ProtectedRoute';

export interface AppState {}

type StoreType = ReturnType<typeof configureStore>;
interface AppProps {
    store: StoreType;
    dispatch: ThunkDispatch<ApplicationState, any, AnyAction>;
}

class AppRoot extends Component<AppProps, AppState> {
    async componentDidMount() {
        this.props.dispatch(init);
    }

    private renderMain() {
        const { store } = this.props;

        return (
            <Provider store={store}>
                <Router>
                    <div className='vssass'>
                        <Route
                            exact
                            path='/'
                            render={
                                // tslint:disable-next-line: react-this-binding-issue
                                () => {
                                    location.href =
                                        'https://devblogs.microsoft.com/visualstudio/intelligent-productivity-and-collaboration-from-anywhere/';
                                    return null;
                                }
                            }
                        />
                        <ProtectedRoute exact path='/environments' component={Main} />
                        <Route exact path='/welcome' component={Welcome} />
                        <ProtectedRoute path='/environment/:id' component={Workbench} />
                    </div>
                </Router>
            </Provider>
        );
    }
    render() {
        return (
            <div className='vssass' key='main-app'>
                {this.renderMain()}
            </div>
        );
    }
}

export const App = connect()(AppRoot);
