import * as React from 'react';

import { PrimaryButton, Stack, Text } from 'office-ui-fabric-react';
import { PortalLayout } from '../portalLayout/portalLayout';
import { BackToEnvironmentsLink } from '../back-to-environments/back-to-environments';
import { Loader } from '../loader/loader';
import { isActivating, stateToDisplayName } from '../../utils/environmentUtils';
import { IWorkbenchSplashScreenProps } from '../../interfaces/IWorkbenchSplashScreenProps';

interface IRenderSplashScreenProps {
    message: string;
}

export const RenderSplashScreen: React.FunctionComponent<IRenderSplashScreenProps> = (props) => {
    return (
        <PortalLayout>
            <Stack
                horizontalAlign='center'
                verticalFill
                verticalAlign='center'
                tokens={{ childrenGap: '20' }}
            >
                <Stack.Item>
                    <Text>{props.message}</Text>
                </Stack.Item>
                <Stack.Item>{props.children}</Stack.Item>
                <Stack.Item>
                    <BackToEnvironmentsLink />
                </Stack.Item>
            </Stack>
        </PortalLayout>
    );
};

export const WorkbenchSplashScreen = (props: IWorkbenchSplashScreenProps) => {
    const { showPrompt, environment, connectError, onRetry, onConnect } = props;

    const { friendlyName } = environment;

    if (connectError !== null) {
        return (
            <RenderSplashScreen
                message={`Connecting to environment ${friendlyName} failed. ${connectError}`}
            >
                <PrimaryButton onClick={onRetry}>Retry</PrimaryButton>
            </RenderSplashScreen>
        );
    }

    const envState = stateToDisplayName(environment.state).toLocaleLowerCase();

    if (showPrompt) {
        return (
            <RenderSplashScreen message={`Environment "${friendlyName}" is ${envState}.`}>
                <PrimaryButton onClick={onConnect}>Connect</PrimaryButton>
            </RenderSplashScreen>
        );
    }

    return (
        <RenderSplashScreen message={`Environment "${friendlyName}" is ${envState}.`}>
            {isActivating(environment) ? <Loader /> : null}
        </RenderSplashScreen>
    );
};
