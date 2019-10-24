import React, { useEffect, useState } from 'react';
import { connect } from 'react-redux';
import { Redirect } from 'react-router-dom';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Image } from 'office-ui-fabric-react/lib/Image';
import { Stack, StackItem } from 'office-ui-fabric-react/lib/Stack';
import { Text } from 'office-ui-fabric-react/lib/Text';
import { PortalLayout } from '../portalLayout/portalLayout';

import { login } from '../../actions/login';

import { ApplicationState } from '../../reducers/rootReducer';
import { Loader } from '../loader/loader';
import { environmentsPath } from '../../routes';
import { ITokenWithMsalAccount } from '../../typings/ITokenWithMsalAccount';
import { setAuthCookie } from '../../utils/setAuthCookie';
import { blogPostUrl } from '../../BlogPost';

import './login.css';
import loginImage from './login-image.png';

interface LoginProps {
    redirectUrl: string | null;
    token: ITokenWithMsalAccount | undefined;
    isAuthenticated: boolean;
    isAuthenticating: boolean;
    login: (...name: Parameters<typeof login>) => void;
}

function withAllowedSubdomain(targetUrl: URL) {
    const [sessionAndPort, app, ...rest] = targetUrl.hostname.split('.');
    if (app === 'app' && rest.join('.') === location.hostname) {
        targetUrl.hostname = [sessionAndPort, app, location.hostname].join('.');

        return targetUrl.toString();
    }
    return new URL(environmentsPath, location.origin).toString();
}

function LoginView(props: LoginProps) {
    const [isAuthCookieSet, setIsAuthCookieSet] = useState(false);

    useEffect(() => {
        if (!props.token) {
            return;
        }

        setAuthCookie(props.token.accessToken).then(
            () => {
                setIsAuthCookieSet(true);
            },
            () => {
                // noop
            }
        );
    }, [setIsAuthCookieSet, props.token]);

    if (!props.isAuthenticated && props.isAuthenticating) {
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

    return (
        <PortalLayout hideNavigation>
            <Stack
                horizontalAlign='center'
                verticalFill
                verticalAlign='center'
                tokens={{ childrenGap: 'l1' }}
                className='login-page'
            >
                <Stack.Item>
                    <Text className='login-page__title'>Visual Studio Online</Text>
                </Stack.Item>
                <Stack.Item>
                    <Text className='login-page__subtitle'>
                        Cloud-powered dev environments accessible from anywhere
                    </Text>
                </Stack.Item>

                <StackItem>
                    <Image src={loginImage} width={326} height={193} />
                </StackItem>

                <Stack.Item>
                    <PrimaryButton onClick={props.login}>Sign in</PrimaryButton>
                </Stack.Item>

                <Stack.Item className='login-page__learn-more-wrapper'>
                    <Link className='login-page__learn-more' href={blogPostUrl}>
                        <span className='login-page__learn-more'>
                            <span>Learn more</span>
                            <span>
                                <Icon
                                    iconName='ChevronRight'
                                    className='login-page__learn-more-icon'
                                />
                            </span>
                        </span>
                    </Link>
                </Stack.Item>
            </Stack>
        </PortalLayout>
    );
}

const getAuthState = (state: ApplicationState) => ({
    redirectUrl: new URLSearchParams(location.search).get('redirectUrl'),
    token: state.authentication.token,
    isAuthenticated: state.authentication.isAuthenticated,
    isAuthenticating: state.authentication.isAuthenticating,
});
const actions = {
    login,
};

export const LoginConnected = connect(
    getAuthState,
    actions
)(LoginView);

export function Login() {
    return <LoginConnected />;
}
