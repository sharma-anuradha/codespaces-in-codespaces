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
import { injectMessageParametersJSX } from './utils/injectMessageParameters';
import classnames from 'classnames';
import { withTranslation, WithTranslation } from 'react-i18next';
import { authService } from './services/authService';
import './loc/i18n'
import './app.css';
import { initializeCodespacePerformanceInstance } from 'vso-workbench';

export interface AppState {
    isMessageBarVisible: boolean;
    isLoggedIn: boolean;
}

type StoreType = ReturnType<typeof configureStore>;
interface AppProps extends WithTranslation {
    store: StoreType;
    configuration: ConfigurationState;
    init: Function;
    routeConfig: unknown[];
}

const isSupported = isSupportedBrowser();
const isPartiallySupported = isPartiallySupportedBrowser();
const partiallySupportedLocalStorageKey = 'vso.suppress.partial.browsersupport.warning';
const notSupportedLocalStorageKey = 'vso.suppress.browsersupport.warning';

initializeCodespacePerformanceInstance();

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
            if (window && window.localStorage.getItem(notSupportedLocalStorageKey) !== 'true') {
                isMessageBarVisible = true;
            }
        }

        this.state = {
            isMessageBarVisible,
            isLoggedIn: false
        };
    }

    async componentDidMount() {
        window.performance.measure(ApplicationLoadEvent.markName);
        telemetry.track(new ApplicationLoadEvent());

        try {
            await this.props.init();
            await authService.getCachedToken()
                .then(cachedToken => {
                    this.setState({
                        isLoggedIn: cachedToken != null
                    });
            });
        } catch {
            // ignore
        }
    }

    private renderMain() {
        const { store, configuration, routeConfig, t: translation} = this.props;
        if (!configuration) {
            return <Loader message={translation('fetchingConfiguration')} translation={translation}/>;
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
        const { t: translation } = this.props;
        const partiallySupportedBrowserBar = (
            <div>
                <MessageBar messageBarType={MessageBarType.warning}>
                    {translation('browserRestrictions')}
                    <Link href='https://aka.ms/vso-browser-support' target='_blank'>
                        {translation('learnMore')}
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
            <div>
                <MessageBar messageBarType={MessageBarType.warning}>
                    Your browser isn’t currently supported in the preview, but we’ll be adding
                    support soon.{' '}
                    <Link
                        onClick={() => {
                            window.localStorage.setItem(notSupportedLocalStorageKey, 'true');
                            this.setState({ isMessageBarVisible: false });
                        }}
                    >
                        Don't show again
                    </Link>
                </MessageBar>
            </div>
        );

        let requestBetaAccess;
        if (this.state.isLoggedIn) {
            requestBetaAccess = (
                <Link href='https://aka.ms/vscs-transition-portal' target='_blank'>
                    {translation('requestBetaAccess')}
                </Link>
            )
        }

        let vsoSunsetBar = (
            <div>
                <MessageBar messageBarType={MessageBarType.warning}>
                    {injectMessageParametersJSX(translation('vsoSunset'),
                        <Link href='https://github.com/features/codespaces' target='_blank'>
                            GitHub
                        </Link>
                    )}
                    {requestBetaAccess}
                    <Link href='https://aka.ms/vscs-moving' target='_blank'>
                        {translation('learnMore')}
                    </Link>
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
            appClassName = classnames('vsonline', 'vso-sunset');
            browserBar = null;
        }

        return (
            <div className={appClassName} key='main-app'>
                {vsoSunsetBar}
                {browserBar}
                {this.renderMain()}
            </div>
        );
    }
}

const getConfig = ({ configuration }: ApplicationState) => ({
    configuration,
});

export const App = withTranslation()(connect(getConfig)(AppRoot));
