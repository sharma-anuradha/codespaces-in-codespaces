import React, { Component } from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';
import { connect, Provider } from 'react-redux';
import './app.css';

import { Main } from './components/main/main';
import { Welcome } from './components/welcome/welcome';
import { Workbench } from './components/workbench/workbench';

import { configureStore } from './store/configureStore';
import { init } from './actions/init';
import { ApplicationState } from './reducers/rootReducer';
import { ProtectedRoute } from './ProtectedRoute';
import { BlogPost } from './BlogPost';
import { Loader } from './components/loader/loader';
import { ConfigurationState } from './reducers/configuration';
import { GitHubLogin } from './components/gitHubLogin/gitHubLogin';

export interface AppState {}

type StoreType = ReturnType<typeof configureStore>;
interface AppProps {
    store: StoreType;
    configuration: ConfigurationState;
    init: typeof init;
}

class AppRoot extends Component<AppProps, AppState> {
    async componentDidMount() {
        this.props.init();
    }

    private renderMain() {
        const { store, configuration } = this.props;

        if (!configuration) {
            return <Loader message='Fetching configuration...' />;
        }

        return (
            <Provider store={store}>
                <Router>
                    <div className='vssass'>
                        <ProtectedRoute path='/environment/:id' component={Workbench} />
                        <ProtectedRoute exact path='/environments' component={Main} />
                        <ProtectedRoute path='/github/login' component={GitHubLogin} />
                        <Route exact path='/welcome' component={Welcome} />
                        <Route exact path='/' component={BlogPost} />
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

const getConfig = ({ configuration }: ApplicationState) => ({
    configuration,
});

export const App = connect(
    getConfig,
    { init }
)(AppRoot);
