import React, { Component, Fragment } from 'react';
import { Redirect } from 'react-router-dom';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { Text } from 'office-ui-fabric-react/lib/Text';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';

import { loginPath, environmentsPath } from '../../routerPaths';
import { loginSilent, acquireTokenRedirect } from '../../actions/login';
import { Loader } from '../loader/loader';
import { clientApplication } from '../../services/msalConfig';
import { PortalLayout } from '../portalLayout/portalLayout';

let loginSilentPromise: any = undefined;

export interface LoginCallbackProps {}

enum LoginState {
    LoginPending = 'Pending',
    LoginPassed = 'Passed',
    LoginFailed = 'Failed',
    LoginFailedMultipleTimes = 'FailedMultipleTimes',
}

interface LoginCallbackState {
    loginState: LoginState;
}

export const INTERACTION_REQUIRED_AUTH_FLAG = 'InteractionRequiredAuthFlag';

export class LoginCallback extends Component<LoginCallbackProps, LoginCallbackState> {
    constructor(props: LoginCallbackProps) {
        super(props);
        this.state = { loginState: LoginState.LoginPending };

        if (!loginSilentPromise) {
            loginSilentPromise = loginSilent().then(
                () => {
                    this.resetInteractionRequiredFlag();
                    this.setState({ loginState: LoginState.LoginPassed });
                },
                (err) => {
                    try {
                        if (err && err.name === 'InteractionRequiredAuthError') {
                            if (this.isInteractionRequiredFlagSet()) {
                                // msal does not support if browser disables cross site cookies https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-js-known-issues-safari-browser
                                this.resetInteractionRequiredFlag();
                                this.setState({ loginState: LoginState.LoginFailedMultipleTimes });
                            } else {
                                this.setInteractionRequiredFlag();
                                acquireTokenRedirect();
                            }
                        } else {
                            this.resetInteractionRequiredFlag();
                            this.setState({ loginState: LoginState.LoginFailed });
                        }
                    } catch (err) {
                        this.resetInteractionRequiredFlag();
                        this.setState({ loginState: LoginState.LoginFailed });
                    }
                }
            );
        }
    }

    render() {
        if (this.state.loginState === LoginState.LoginPassed) {
            return <Redirect to={environmentsPath} />;
        } else if (this.state.loginState === LoginState.LoginFailed) {
            return <Redirect to={loginPath} />;
        } else if (this.state.loginState === LoginState.LoginFailedMultipleTimes) {
            return (
                <PortalLayout hideNavigation>
                    <Stack
                        horizontalAlign='center'
                        verticalFill
                        verticalAlign='center'
                        tokens={{ childrenGap: 'l1' }}
                    >
                        <Fragment>
                            <Stack.Item>
                                <Text>
                                    Login failed due to an unexpected error. Please log out and try
                                    again.
                                </Text>
                            </Stack.Item>
                            <Stack.Item>
                                <PrimaryButton onClick={() => this.logout()}>Log out</PrimaryButton>
                            </Stack.Item>
                        </Fragment>
                    </Stack>
                </PortalLayout>
            );
        }
        return <Loader message='Signing in...' />;
    }

    private resetInteractionRequiredFlag() {
        localStorage.removeItem(INTERACTION_REQUIRED_AUTH_FLAG);
    }

    private isInteractionRequiredFlagSet(): boolean {
        let errorCount = localStorage.getItem(INTERACTION_REQUIRED_AUTH_FLAG);
        return !!errorCount;
    }

    private setInteractionRequiredFlag() {
        localStorage.setItem(INTERACTION_REQUIRED_AUTH_FLAG, 'true');
    }

    private logout() {
        this.resetInteractionRequiredFlag();
        if (clientApplication) {
            clientApplication.logout();
        }
        location.href = loginPath;
    }
}
