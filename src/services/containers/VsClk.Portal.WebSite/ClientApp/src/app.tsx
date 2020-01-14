import React, { Component } from 'react';
import { BrowserRouter as Router, Route, Switch } from 'react-router-dom';
import { connect, Provider } from 'react-redux';
import { configureStore } from './store/configureStore';
import { ApplicationState } from './reducers/rootReducer';
import { Loader } from './components/loader/loader';
import { ConfigurationState } from './reducers/configuration';

import { telemetry } from './utils/telemetry';
import { ApplicationLoadEvent } from './utils/telemetry/ApplicationLoadEvent';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react';
import { isSupportedBrowser } from './utils/detection';
import classnames from 'classnames';

import './app.css';

export interface AppState {}

type StoreType = ReturnType<typeof configureStore>;
interface AppProps {
    store: StoreType;
    configuration: ConfigurationState;
    init: Function;
    routeConfig: unknown[];
}

const isSupported = isSupportedBrowser();

class AppRoot extends Component<AppProps, AppState> {
    async componentDidMount() {
        window.performance.measure(ApplicationLoadEvent.markName);
        telemetry.track(new ApplicationLoadEvent());

        try {
            await this.props.init();
        } catch {
            // ignore
        }
    }

    private renderMain() {
        const {
            store,
            configuration,
            routeConfig
        } = this.props;

        if (!configuration) {
            return <Loader message='Fetching configuration...' />;
        }

        return (
            <Provider store={store}>
                <Router>
                    <div className='vsonline'>
                        <Switch>{routeConfig}</Switch>
                    </div>
                </Router>
            </Provider>
        );
    }

    render() {
        const unsupportedBrowserBar = (
            <div style={{ margin: '0 -.8rem' }}>
                <MessageBar messageBarType={MessageBarType.warning}>
                    Your browser isn’t currently supported in the preview, but we’ll be adding
                    support soon.
                </MessageBar>
            </div>
        );

        const appClassName = classnames('vsonline', !isSupported && 'supported-browser');

        return (
            <div className={appClassName} key='main-app'>
                {!isSupported && unsupportedBrowserBar}
                {this.renderMain()}
            </div>
        );
    }
}

const getConfig = ({ configuration }: ApplicationState) => ({
    configuration,
});

export const App = connect(
    getConfig
)(AppRoot);
