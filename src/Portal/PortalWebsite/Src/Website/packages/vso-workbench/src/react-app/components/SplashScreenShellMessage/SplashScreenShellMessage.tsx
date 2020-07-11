import * as React from 'react';

import classNames from 'classnames';

import { ISplashScreenProps } from '../../../interfaces/ISplashScreenProps';

import { Spinner } from '../Spinner/Spinner';
import { IButtonLinkProps, ButtonLink } from '../ButtonLink/ButtonLink';
import { RenderSplashScreen } from '../SplashScreenShell/RenderSplashScreen';

import './SplashScreenShellMessage.css'


interface ISplashScreenMessageProps extends ISplashScreenProps {
    message: string;
    button?: IButtonLinkProps;
    isLightTheme: boolean;
    isSpinner?: boolean;
    isSpinnerStopped?: boolean;
}

export const MaybeSpinner: React.FunctionComponent<ISplashScreenMessageProps> = (props: ISplashScreenMessageProps) => {
    if (props.isSpinner) {
        return (<Spinner isStopped={props.isSpinnerStopped} />);
    }

    return null;
};

export const MaybeButton: React.FunctionComponent<ISplashScreenMessageProps> = (props: ISplashScreenMessageProps) => {
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

export const SplashScreenMessage: React.FunctionComponent<ISplashScreenMessageProps> = (props: ISplashScreenMessageProps) => {
    const { isLightTheme } = props;

    const mainClassName = classNames({
        'vso-splash-screen__block': true,
        'vso-splash-screen__block--light-theme': isLightTheme,
    });

    return (
        <RenderSplashScreen isOnVSCodespaces={!props.isLightTheme}>
            <div className={mainClassName}>
                <div className="vso-splash-screen__message">
                    <MaybeSpinner {...props} /> { props.message }
                </div>
                <MaybeButton {...props} />
            </div>
        </RenderSplashScreen>
    );
};
