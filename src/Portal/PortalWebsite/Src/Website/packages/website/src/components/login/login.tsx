import React, { useEffect, useState, useCallback, Fragment } from 'react';
import { connect } from 'react-redux';
import { Redirect } from 'react-router-dom';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Stack, StackItem } from 'office-ui-fabric-react/lib/Stack';
import { Text } from 'office-ui-fabric-react/lib/Text';

import { Signal, createTrace } from 'vso-client-core';

import { PortalLayout } from '../portalLayout/portalLayout';
import { login, complete2FA } from '../../actions/login';
import { ApplicationState } from '../../reducers/rootReducer';
import { Loader } from '../loader/loader';
import { environmentsPath } from '../../routerPaths';
import { EverywhereImage } from '../EverywhereImage/EverywhereImage';
import { blogPostUrl, pricingInfoUrl, privacyStatementUrl } from '../../constants';

import './login.css';

const trace = createTrace('Login');

interface LoginProps {
    redirectUrl: string | null;
    token?: string;
    isAuthenticated: boolean;
    isAuthenticating: boolean;
    isInteractionRequired: boolean;
    complete2FA: () => void;
    login: (...name: Parameters<typeof login>) => Promise<unknown>;
}

function withAllowedSubdomain(targetUrl: URL) {
    const [sessionAndPort, app, ...rest] = targetUrl.hostname.split('.');
    if (app === 'app' && rest.join('.') === location.hostname) {
        targetUrl.hostname = [sessionAndPort, app, location.hostname].join('.');

        return targetUrl.toString();
    }
    return new URL(environmentsPath, location.origin).toString();
}

function addCookieConsentCookie() {
    const currentTimestamp = Math.floor(Date.now() / 1000);
    const oneYearFromNow = new Date(new Date().setFullYear(new Date().getFullYear() + 1));
    // tslint:disable-next-line:no-cookies
    document.cookie = `MSCC=${currentTimestamp};expires=${oneYearFromNow.toString()};`;
}

const LoginPageSignInForm = (props: LoginProps) => {
    const loginClick = useCallback(() => {
        addCookieConsentCookie(); // Workaround to add MSCC cookie for SPA
        props.login().catch((error) => {
            trace.error('Login failed', { error });
        });
    }, [props.login]);

    return (
        <Fragment>
            <Stack.Item>
                <Text className='login-page__title'>Visual Studio Codespaces</Text>
            </Stack.Item>
            <Stack.Item>
                <Text className='login-page__subtitle'>
                    Cloud-powered dev environments accessible from anywhere
                </Text>
            </Stack.Item>

            <StackItem>
                <EverywhereImage />
            </StackItem>

            <Stack.Item>
                <PrimaryButton onClick={loginClick} className='login-page__login-button'>
                    Sign in
                </PrimaryButton>
            </Stack.Item>
            <Stack.Item className='login-page__learn-more-wrapper'>
                <Link className='login-page__learn-more' href={blogPostUrl}>
                    <span className='login-page__learn-more'>
                        <span>Learn more</span>
                        <span>
                            <Icon iconName='ChevronRight' className='login-page__learn-more-icon' />
                        </span>
                    </span>
                </Link>
                <Link className='login-page__learn-more' href={pricingInfoUrl}>
                    <span className='login-page__learn-more'>
                        <span>Pricing</span>
                        <span>
                            <Icon iconName='ChevronRight' className='login-page__learn-more-icon' />
                        </span>
                    </span>
                </Link>
                <Link className='login-page__learn-more' href={privacyStatementUrl}>
                    <span className='login-page__learn-more'>
                        <span>Privacy notice</span>
                        <span>
                            <Icon iconName='ChevronRight' className='login-page__learn-more-icon' />
                        </span>
                    </span>
                </Link>
            </Stack.Item>
        </Fragment>
    );
};

const LoginPage2FAStepForm = (props: LoginProps) => {
    return (
        <Fragment>
            <Stack.Item>
                <PrimaryButton onClick={props.complete2FA} className='login-page__login-button'>
                    Complete 2-factor authentication
                </PrimaryButton>
            </Stack.Item>
        </Fragment>
    );
};

const LoginForm = (props: LoginProps) => {
    if (!props.isInteractionRequired) {
        return <LoginPageSignInForm {...props} />;
    }

    return <LoginPage2FAStepForm {...props} />;
};

// tslint:disable-next-line: max-func-body-length
function LoginView(props: LoginProps) {
    const [markupHtml, setMarkupHtml] = useState('');
    useEffect(() => {
        const cookieConsentSignal = Signal.from(
            fetch('/cookie-consent', {
                method: 'GET',
            })
        );

        cookieConsentSignal.promise.then(
            async (response) => {
                const mscc = await response.json();
                if (mscc) {
                    setMarkupHtml(mscc.Markup);
                }
            },
            () => {
                // noop
            }
        );

        return () => {
            cookieConsentSignal.cancel();
        };
    }, [setMarkupHtml]);

    const [isAuthCookieSet] = useState(false);

    const { isAuthenticated, isAuthenticating, isInteractionRequired } = props;
    if (!isAuthenticated && isAuthenticating && !isInteractionRequired) {
        return <Loader message='Signing in...' />;
    }
    if (props.isAuthenticated) {
        if (!props.redirectUrl) {
            return <Redirect to={environmentsPath} />;
        }

        const redirectUrl = new URL(props.redirectUrl, location.origin);
        if (redirectUrl.origin === location.origin) {
            return <Redirect to={redirectUrl.toString().substr(redirectUrl.origin.length)} />;
        }

        if (isAuthCookieSet) {
            window.location.href = withAllowedSubdomain(redirectUrl);
        } else {
            return <Loader message='Signing in...' />;
        }
    }

    const markup = { __html: markupHtml };

    return (
        <PortalLayout hideNavigation>
            <div className='ms-Grid-row' dangerouslySetInnerHTML={markup}></div>
            <Stack
                horizontalAlign='center'
                verticalFill
                verticalAlign='center'
                tokens={{ childrenGap: 'l1' }}
                className='login-page'
            >
                <LoginForm {...props} />
            </Stack>
        </PortalLayout>
    );
}

const getAuthState = (state: ApplicationState) => ({
    redirectUrl: new URLSearchParams(location.search).get('redirectUrl'),
    ...state.authentication,
});
const actions = {
    login,
    complete2FA,
};

export const LoginConnected = connect(getAuthState, actions)(LoginView);

export function Login() {
    return <LoginConnected />;
}