import React, { Component } from 'react';
import { BrowserRouter as Router, Switch } from 'react-router-dom';
import { connect, Provider } from 'react-redux';
import { configureStore } from './store/configureStore';
import { ApplicationState } from './reducers/rootReducer';
import { Loader } from './components/loader/loader';
import { ConfigurationState } from './reducers/configuration';

import { telemetry } from './utils/telemetry';
import { ApplicationLoadEvent } from './utils/telemetry/ApplicationLoadEvent';
import { MessageBar, MessageBarType, Link } from 'office-ui-fabric-react';
import { isSupportedBrowser, isPartiallySupportedBrowser } from './utils/detection';
import classnames from 'classnames';

import './app.css';

export interface AppState {
    isMessageBarVisible: boolean;
}

type StoreType = ReturnType<typeof configureStore>;
interface AppProps {
    store: StoreType;
    configuration: ConfigurationState;
    init: Function;
    routeConfig: unknown[];
}

const isSupported = isSupportedBrowser();
const isPartiallySupported = isPartiallySupportedBrowser();
const partiallySupportedLocalStorageKey = 'vso.suppress.partial.browsersupport.warning';

class AppRoot extends Component<AppProps, AppState> {
    constructor(props: AppProps) {
        super(props);
        let isMessageBarVisible: boolean = false;
        if (isPartiallySupported) {
            if (
                window &&
                window.localStorage.getItem(partiallySupportedLocalStorageKey) !== 'true'
            ) {
                isMessageBarVisible = true;
            }
        } else if (!isSupported) {
            isMessageBarVisible = true;
        }

        this.state = {
            isMessageBarVisible,
        };
    }

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
        const { store, configuration, routeConfig } = this.props;

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
        const partiallySupportedBrowserBar = (
            <div>
                <MessageBar messageBarType={MessageBarType.warning}>
                    Some features might not work because of browser restrictions -
                    <Link href='https://aka.ms/vso-browser-support' target='_blank'>
                        Learn more
                    </Link>
                    {'. '}
                    <Link
                        onClick={() => {
                            window.localStorage.setItem(partiallySupportedLocalStorageKey, 'true');
                            this.setState({ isMessageBarVisible: false });
                        }}
                    >
                        Don't show again
                    </Link>
                </MessageBar>
            </div>
        );

        const unsupportedBrowserBar = (
            <div style={{ margin: '0 -.8rem' }}>
                <MessageBar messageBarType={MessageBarType.warning}>
                    Your browser isn’t currently supported in the preview, but we’ll be adding
                    support soon.
                </MessageBar>
            </div>
        );

        var appClassName = '';
        var browserBar;

        if (this.state.isMessageBarVisible) {
            appClassName = classnames('vsonline', 'supported-browser');
            browserBar = isPartiallySupported
                ? partiallySupportedBrowserBar
                : unsupportedBrowserBar;
        } else {
            appClassName = 'vsonline';
            browserBar = null;
        }

        return (
            <div className={appClassName} key='main-app'>
                {browserBar}
                {this.renderMain()}
            </div>
        );
    }
}

const getConfig = ({ configuration }: ApplicationState) => ({
    configuration,
});

export const App = connect(getConfig)(AppRoot);
