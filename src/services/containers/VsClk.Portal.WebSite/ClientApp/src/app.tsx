import React, { Component } from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';
import { connect, Provider } from 'react-redux';
import { configureStore } from './store/configureStore';
import { init } from './actions/init';
import { ApplicationState } from './reducers/rootReducer';
import { ProtectedRoute } from './ProtectedRoute';
import { Loader } from './components/loader/loader';
import { ConfigurationState } from './reducers/configuration';

import { routes } from './routes';
import { telemetry } from './utils/telemetry';
import { ApplicationLoadEvent } from './utils/telemetry/ApplicationLoadEvent';

import './app.css';

export interface AppState {}

type StoreType = ReturnType<typeof configureStore>;
interface AppProps {
    store: StoreType;
    configuration: ConfigurationState;
    init: typeof init;
}

class AppRoot extends Component<AppProps, AppState> {
    async componentDidMount() {
        window.performance.measure(ApplicationLoadEvent.markName);
        telemetry.track(new ApplicationLoadEvent());

        this.props.init();
    }

    private renderMain() {
        const { store, configuration } = this.props;

        if (!configuration) {
            return <Loader message='Fetching configuration...' />;
        }

        const routeConfig = routes.map((r, i) => {
            const { authenticated, ...props } = r;
            return authenticated ? (
                <ProtectedRoute {...props} key={i} />
            ) : (
                <Route {...props} key={i} />
            );
        });

        return (
            <Provider store={store}>
                <Router>
                    <div className='vssass'>{routeConfig}</div>
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
