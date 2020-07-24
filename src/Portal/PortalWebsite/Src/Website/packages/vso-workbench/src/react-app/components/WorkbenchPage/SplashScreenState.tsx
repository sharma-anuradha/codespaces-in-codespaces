import React from 'react';

import { EnvironmentStateInfo, randomString, IEnvironment, IPartnerInfo } from 'vso-client-core';
import { VSOSplashScreen } from '@vs/vso-splash-screen';

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
import { VSCodespacesPlatformInfo } from 'vs-codespaces-authorization';
import { GitCredentialService } from '../../../rpcServices/GitCredentialService';
import { BrowserSyncService } from '../../../rpcServices/BrowserSyncService';

interface ISplashScreenProps {
    environmentInfo: IEnvironment | null;
    platformInfo: IPartnerInfo | VSCodespacesPlatformInfo | null;
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
        GitCredentialService
    );

    const token = await authService.getCachedCodespaceToken();

    if (!token) {
        throw new Error('Cannot get Codespace token.');
    }

    await result.connect(environmentInfo.connection.sessionId, token);
};

const isLightThemeColor = (
    platformInfo: IPartnerInfo | VSCodespacesPlatformInfo | null
): boolean => {
    if (!platformInfo) {
        return true;
    }

    if ('vscodeSettings' in platformInfo) {
        const { vscodeSettings } = platformInfo;
        const { loadingScreenThemeColor } = vscodeSettings;

        return !loadingScreenThemeColor || (loadingScreenThemeColor === 'light');
    }

    return true;
};


export const SplashScreenState: React.FunctionComponent<ISplashScreenProps> = (
    props: ISplashScreenProps
) => {
    const {
        environmentState,
        onSignIn,
        startEnvironment,
        message,
        environmentInfo,
        platformInfo,
    } = props;

    const params = new URLSearchParams(location.search);
    const themeParams = params.get('loadingScreenThemeColor');
    let isLightTheme = undefined;

    if (!themeParams) {
        isLightTheme = isLightThemeColor(platformInfo);      
    } else {
        switch (themeParams) {
            case 'dark':
                isLightTheme = false;
                break;
            case 'light':
                isLightTheme = true;
                break;
            default:
                isLightTheme = isLightThemeColor(platformInfo);
                break;
        }
    }

    switch (environmentState) {
        case EnvironmentWorkspaceState.Unknown:
        case EnvironmentWorkspaceState.Initializing: {
            return (
                <SplashScreenMessage
                    message='Getting things ready...'
                    isLightTheme={isLightTheme}
                />
            );
        }
        case EnvironmentWorkspaceState.SignedOut: {
            const buttonProps: IButtonLinkProps = {
                text: 'Sign in',
                onClick: onSignIn,
            };
            return (
                <SplashScreenMessage
                    message='Please sign in to proceed.'
                    button={buttonProps}
                    isLightTheme={isLightTheme}
                />
            );
        }
        case EnvironmentWorkspaceState.Error: {
            return (
                <SplashScreenMessage
                    message={message || 'Codespace error.'}
                    isLightTheme={isLightTheme}
                />
            );
        }
        case EnvironmentStateInfo.Starting: {
            return (
                <SplashScreenMessage
                    message='Starting the codespace...'
                    isLightTheme={isLightTheme}
                />
            );
        }
        case EnvironmentStateInfo.Deleted: {
            return (
                <SplashScreenMessage
                    message='The codespace has been deleted.'
                    isLightTheme={isLightTheme}
                />
            );
        }
        case EnvironmentStateInfo.Failed: {
            return (
                <SplashScreenMessage message='The codespace failed.' isLightTheme={isLightTheme} />
            );
        }
        case EnvironmentStateInfo.Provisioning: {
            if (!environmentInfo) {
                return (
                    <SplashScreenMessage
                        message='Getting things ready...'
                        isLightTheme={isLightTheme}
                    />
                );
            }

            const connection = React.useMemo(() => {
                return new ConnectionAdapter();
            }, []);

            React.useMemo(async () => {
                await connectSplashScreen(environmentInfo);
            }, []);

            return (
                <SplashScreenShell isGithubSplashScreen={isLightTheme}>
                    <VSOSplashScreen connection={connection} />
                </SplashScreenShell>
            );
        }
        case EnvironmentStateInfo.Available: {
            return <SplashScreenMessage message='Connecting...' isLightTheme={isLightTheme} />;
        }
        case EnvironmentStateInfo.ShuttingDown: {
            return (
                <SplashScreenMessage
                    message='The codespace is shutting down.'
                    isLightTheme={isLightTheme}
                />
            );
        }
        case EnvironmentStateInfo.Shutdown: {
            return (
                <SplashScreenMessage
                    message='The codespace is shutdown.'
                    isLightTheme={isLightTheme}
                    button={{
                        text: 'Connect',
                        onClick: startEnvironment,
                    }}
                />
            );
        }
        case EnvironmentStateInfo.Unavailable: {
            return (
                <SplashScreenMessage
                    message='The codespace is not available.'
                    isLightTheme={isLightTheme}
                />
            );
        }
        default: {
            return (
                <SplashScreenMessage
                    message='Unknown codespace state.'
                    isLightTheme={isLightTheme}
                />
            );
        }
    }
};
