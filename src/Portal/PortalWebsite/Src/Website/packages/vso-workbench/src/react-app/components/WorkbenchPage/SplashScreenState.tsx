import React from 'react';

import { EnvironmentStateInfo, randomString, IEnvironment } from 'vso-client-core';
import { VSOSplashScreen } from '@vs/vso-splash-screen';
import { BrowserSyncService, GitCredentialService } from 'vso-ts-agent';

import { SplashScreenMessage } from '../SplashScreenShellMessage/SplashScreenShellMessage';
import { IButtonLinkProps } from '../ButtonLink/ButtonLink';
import { EnvironmentWorkspaceState } from '../../../interfaces/EnvironmentWorkspaceState';
import { TEnvironmentState } from '../../../interfaces/TEnvironmentState';
import { SplashScreenShell } from '../SplashScreenShell/SplashScreenShell';
import { ConnectionAdapter } from '../SplashScreenShell/ConnectionAdapter';
import { CommunicationAdapter } from '../../../utils/splashScreen/communicationAdapter';
import { SplashCommunicationProvider } from '../../../utils/splashScreen/SplashScreenCommunicationProvider';
import { config } from '../../../config/config';
import { authService } from '../../../auth/authService';

interface ISplashScreenState {
    environmentInfo: IEnvironment | null;
    environmentState: TEnvironmentState;
    message?: string;
    startEnvironment: () => any;
    onSignIn: () => any;
}

const connectSplashScreen = async (environmentInfo: IEnvironment) => {
    const connectionAdapter = new SplashCommunicationProvider(async (command: any) => {});

    const result = new CommunicationAdapter(
        connectionAdapter,
        config.liveShareApi,
        randomString(),
        BrowserSyncService,
        GitCredentialService,
    );

    const token = await authService.getCachedCodespaceToken();

    if (!token) {
        throw new Error('Cannot get Codespace token.');
    }

    await result.connect(environmentInfo.connection.sessionId, token);
}

export const SplashScreenState: React.FunctionComponent<ISplashScreenState> = (
    props: ISplashScreenState
) => {
    const {
        environmentState,
        onSignIn,
        startEnvironment,
        message,
        environmentInfo
    } = props;
    
    switch (environmentState) {
        case EnvironmentWorkspaceState.Unknown:
        case EnvironmentWorkspaceState.Initializing: {
            return <SplashScreenMessage message='Getting things ready...' />;
        }
        case EnvironmentWorkspaceState.SignedOut: {
            const buttonProps: IButtonLinkProps = {
                text: 'Sign in',
                onClick: onSignIn,
            };
            return (
                <SplashScreenMessage message='Please sign in to proceed.' button={buttonProps} />
            );
        }
        case EnvironmentWorkspaceState.Error: {
            return <SplashScreenMessage message={message || 'Codespace error.'} />;
        }
        case EnvironmentStateInfo.Starting: {
            return <SplashScreenMessage message='Starting the codespace...' />;
        }
        case EnvironmentStateInfo.Deleted: {
            return <SplashScreenMessage message='The codespace has been deleted.' />;
        }
        case EnvironmentStateInfo.Failed: {
            return <SplashScreenMessage message='The codespace failed.' />;
        }
        case EnvironmentStateInfo.Provisioning: {
            if (!environmentInfo) {
                return <SplashScreenMessage message='Getting things ready...' />;
            }

            const connection = React.useMemo(() => {
                return new ConnectionAdapter();
            }, []);

            React.useMemo(async () => {
                await connectSplashScreen(environmentInfo);
            }, []);

            return (
                <SplashScreenShell isGithubSplashScreen={false}>
                    <VSOSplashScreen connection={connection} />
                </SplashScreenShell>
            );
        }
        case EnvironmentStateInfo.Available: {
            return <SplashScreenMessage message='Connecting...' />;
        }
        case EnvironmentStateInfo.ShuttingDown: {
            return <SplashScreenMessage message='The codespace is shutting down.' />;
        }
        case EnvironmentStateInfo.Shutdown: {
            return (
                <SplashScreenMessage
                    message='The codespace is shutdown.'
                    button={{
                        text: 'Connect',
                        onClick: startEnvironment,
                    }}
                />
            );
        }
        case EnvironmentStateInfo.Unavailable: {
            return <SplashScreenMessage message='The codespace is not available.' />;
        }
        default: {
            return <SplashScreenMessage message='Unknown codespace state.' />;
        }
    }
};
