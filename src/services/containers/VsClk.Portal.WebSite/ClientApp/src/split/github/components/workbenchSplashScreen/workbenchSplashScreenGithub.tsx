import * as React from 'react';

import { PrimaryButton, Stack, Text } from 'office-ui-fabric-react';
import { PortalLayout } from '../../../../components/portalLayout/portalLayout';
import { Loader } from '../../../../components/loader/loader';
import { isActivating, stateToDisplayName } from '../../../../utils/environmentUtils';
import { IWorkbenchSplashScreenProps } from '../../../../interfaces/IWorkbenchSplashScreenProps';

interface IRenderSplashScreenProps {
    message: string;
}

export const RenderSplashScreen: React.FunctionComponent<IRenderSplashScreenProps> = (props) => {
    return (
        <PortalLayout hideNavigation>
            <Stack
                horizontalAlign='center'
                verticalFill
                verticalAlign='center'
                tokens={{ childrenGap: '20' }}
            >
                <Stack.Item>
                    <Text>{props.message}</Text>
                </Stack.Item>
                <Stack.Item>
                    {props.children}
                </Stack.Item>
            </Stack>
        </PortalLayout>
    );
}

export const WorkbenchSplashScreenGithub = (props: IWorkbenchSplashScreenProps) => {
    const {
        environment,
        connectError,
        onRetry
    } = props;

    const { friendlyName } = environment;

    if (connectError !== null) {
        return (
            <RenderSplashScreen message={`Connecting to Codespace ${friendlyName} failed. ${connectError}`}>
                <PrimaryButton onClick={onRetry}>Retry</PrimaryButton>
            </RenderSplashScreen>
        );
    }

    const envState = stateToDisplayName(environment.state).toLocaleLowerCase();
    return (
        <RenderSplashScreen message={`Codespace "${friendlyName}" is ${envState}.`}>
            {
                isActivating(environment)
                    ? <Loader />
                    : null
            }
        </RenderSplashScreen>
    );
}
