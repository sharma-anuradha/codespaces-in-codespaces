import * as React from 'react';

import { EnvironmentStateInfo } from 'vso-client-core';
import { VSOSplashScreen } from '@vs/vso-splash-screen';

import { Workbench } from '../Workbench/Workbench';
import { SplashScreenMessage } from '../SplashScreenShellMessage/SplashScreenShellMessage';
import { IButtonLinkProps } from '../ButtonLink/ButtonLink';
import { EnvironmentWorkspaceState } from '../../../interfaces/EnvironmentWorkspaceState';
import { TEnvironmentState } from '../../../interfaces/TEnvironmentState';
import { ConnectionAdapter } from './ConnectionAdapter';
import { SplashScreenShell } from '../SplashScreenShell/SplashScreenShell';

export interface IWorkbenchPageRenderProps {
    state: TEnvironmentState;
    message?: string;
    startEnvironment: () => any;
    onSignIn: () => any;
    handleAPIError: (e: Error) => any;
}

export const WorkbenchPageRender: React.FunctionComponent<IWorkbenchPageRenderProps> = (
    props: IWorkbenchPageRenderProps
) => {
    const { state, message, startEnvironment, handleAPIError, onSignIn } = props;

    switch (state) {
        case EnvironmentWorkspaceState.Unknown:
        case EnvironmentWorkspaceState.Initializing: {
            return (
                <SplashScreenMessage
                    message='Getting things ready...'
                    isSpinner={true}
                    isSpinnerStopped={true}
                />
            );
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
            return <SplashScreenMessage message={message || 'Workspace error.'} />;
        }

        case EnvironmentStateInfo.Starting: {
            return <SplashScreenMessage message='Starting the workspace...' isSpinner={true} />;
        }

        case EnvironmentStateInfo.Deleted: {
            return <SplashScreenMessage message='The workspace has been deleted.' />;
        }

        case EnvironmentStateInfo.Failed: {
            return <SplashScreenMessage message='The workspace failed.' />;
        }

        case EnvironmentStateInfo.Provisioning: {
            const connection = React.useMemo(() => {
                return new ConnectionAdapter();
            }, []);
            return (
                <SplashScreenShell>
                    <VSOSplashScreen connection={connection} />
                </SplashScreenShell>
            );
        }

        case EnvironmentStateInfo.ShuttingDown: {
            return (
                <SplashScreenMessage message='The workspace is shutting down.' isSpinner={true} />
            );
        }

        case EnvironmentStateInfo.Shutdown: {
            return (
                <SplashScreenMessage
                    message='The workspace is shutdown.'
                    button={{
                        text: 'Connect',
                        onClick: startEnvironment,
                    }}
                />
            );
        }

        case EnvironmentStateInfo.Unavailable: {
            return <SplashScreenMessage message='The workspace is not available.' />;
        }

        case EnvironmentStateInfo.Available: {
            return <Workbench onError={handleAPIError} />;
        }

        default: {
            return <SplashScreenMessage message='Unknown workspace state.' />;
        }
    }
};
