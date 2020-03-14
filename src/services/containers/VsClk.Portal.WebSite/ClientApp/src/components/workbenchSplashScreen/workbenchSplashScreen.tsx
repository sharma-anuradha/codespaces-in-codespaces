import * as React from 'react';

import { PrimaryButton, Stack, Text } from 'office-ui-fabric-react';
import { PortalLayout } from '../portalLayout/portalLayout';
import { BackToEnvironmentsLink } from '../back-to-environments/back-to-environments';
import { VSOSplashScreen, IConnectionAdapter } from '@vs/vso-splash-screen'
import { stateToDisplayName } from '../../utils/environmentUtils';
import { IWorkbenchSplashScreenProps } from '../../interfaces/IWorkbenchSplashScreenProps';
import './workbenchSplashScreen.css';

interface IRenderSplashScreenProps {
    message: string;
}

class CommunicationProvider implements IConnectionAdapter {
    sendCommand(command: string, args: { environmentId: string }) {
        switch(command) {
            case 'connect':
                    window.postMessage({
                        command,
                        environmentId: args.environmentId,
                    }, window.origin);
                break;
        }
    }

    onMessage(callback: (ev: MessageEvent) => any) {
        window.addEventListener("message", callback, false);
    }
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

export const WorkbenchSplashScreen: React.FC<IWorkbenchSplashScreenProps> = (props: IWorkbenchSplashScreenProps) => {
    const connection = React.useMemo(() => { return new CommunicationProvider() }, []);
    const { showPrompt, environment, connectError, onRetry, onConnect, title } = props;
    const { friendlyName } = environment;

    if (connectError !== null) {
        return (
            <RenderSplashScreen message={`Connecting to environment ${friendlyName} failed. ${connectError}`}>
                <PrimaryButton onClick={onRetry}>Retry</PrimaryButton>
            </RenderSplashScreen>
        );
    }
    
        const envState = stateToDisplayName(environment!.state).toLocaleLowerCase();
        if (showPrompt) {
            return (
                <RenderSplashScreen message={`Environment "${friendlyName}" is ${envState}.`}>
                    <PrimaryButton onClick={onConnect}>Connect</PrimaryButton>
                </RenderSplashScreen>
            );
        } else {
            return (
                    <div className="vsonline-splash-screen-main">
                        <div className="vsonline-splash-screen-extensions-pane"></div>
                        <div className='vsonline-splash-screen-tree-pane'></div>
                        <div className="vsonline-splash-screen-editor">
                            <div className='vsonline-splash-screen-titlebar'>
                                <div className='vsonline-splash-screen-titlebar-tab'></div>
                            </div>
                            <div className="vsonline-splash-screen-body">
                                <div className="vsonline-splash-screen-preparation-container">
                                    <VSOSplashScreen connection={connection} title={title}></VSOSplashScreen>
                                </div>
                            </div>
                        </div>
                    </div>
                    
            );
        }
}
