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
import { Loader } from './components/loader/loader';

export interface AppState {
    isLoading: boolean;
}

type StoreType = ReturnType<typeof configureStore>;
interface AppProps {
    store: StoreType;
    dispatch: ThunkDispatch<ApplicationState, any, AnyAction>;
}

class AppRoot extends Component<AppProps, AppState> {
    constructor(props: AppProps) {
        super(props);

        this.state = {
            isLoading: true,
        };
    }

    async componentDidMount() {
        this.setState({
            isLoading: false,
        });

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
                        <Route exact path='/environments' component={Main} />
                        <Route exact path='/welcome' component={Welcome} />
                        <Route path='/environment/:id' component={Workbench} />
                    </div>
                </Router>
            </Provider>
        );
    }
    render() {
        const { isLoading } = this.state;

        return (
            <div className='vssass' key='main-app'>
                {isLoading ? <Loader message='Signing in...' /> : this.renderMain()}
            </div>
        );
    }
}

export const App = connect()(AppRoot);
