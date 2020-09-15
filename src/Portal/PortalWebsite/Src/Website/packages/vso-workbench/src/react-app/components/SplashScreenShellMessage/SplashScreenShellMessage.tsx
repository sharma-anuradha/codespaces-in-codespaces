import * as React from 'react';
import { FunctionComponent } from 'react';

import { ISplashScreenProps } from '../../../interfaces/ISplashScreenProps';
import { isHostedOnGithub } from 'vso-client-core';

import { IButtonLinkProps, ButtonLink } from '../ButtonLink/ButtonLink';
import { RenderSplashScreen } from '../SplashScreenShell/RenderSplashScreen';
import { Icon } from '../Icon/Icon';

export type TMessageIcon = 'success' | 'error' | 'progress';

interface ISplashScreenMessageProps extends ISplashScreenProps {
    message: string;
    button?: IButtonLinkProps;
    messageIcon?: TMessageIcon;
    isLightTheme: boolean;
}

export const MaybeIcon: FunctionComponent<ISplashScreenMessageProps> = (
    props: ISplashScreenMessageProps
) => {
    if (!props.messageIcon) {
        return null;
    }

    switch (props.messageIcon) {
        case 'progress': {
            return (
                <Icon
                    type='spinner'
                    className='vso-splash-screen__status vso-splash-screen__status-icon-progress'
                />
            );
        }

        case 'success': {
            return <Icon type='tick' color='green' />;
        }

        case 'error': {
            return <Icon type='error' color='red' />;
        }

        default: {
            throw new Error('Unknown icon state.');
        }
    }
};

export const MaybeButton: FunctionComponent<ISplashScreenMessageProps> = (
    props: ISplashScreenMessageProps
) => {
    if (props.button) {
        return (
            <div className='vso-splash-screen__button'>
                <ButtonLink
                    text={props.button.text}
                    url={props.button.url}
                    onClick={props.button.onClick}
                />
            </div>
        );
    }

    return null;
};

export const SplashScreenMessage: FunctionComponent<ISplashScreenMessageProps> = (
    props: ISplashScreenMessageProps
) => {
    const { isLightTheme } = props;

    return (
        <RenderSplashScreen isLightTheme={isLightTheme} isLogo={isHostedOnGithub()}>
            <div className='vso-splash-screen__steps'>
                <h4>Preparing your Codespace</h4>
                <ul>
                    <li>
                        <div className='vso-splash-screen__step-header'>
                            <MaybeIcon {...props} /> <span className='title'>{props.message}</span>
                            <MaybeButton {...props} />
                        </div>
                    </li>
                </ul>
            </div>
        </RenderSplashScreen>
    );
};
