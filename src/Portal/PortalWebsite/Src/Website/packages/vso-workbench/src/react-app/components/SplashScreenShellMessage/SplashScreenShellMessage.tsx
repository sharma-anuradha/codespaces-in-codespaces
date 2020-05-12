import * as React from 'react';
import { Fragment } from 'react';
import { SplashScreenShell } from '../SplashScreenShell/SplashScreenShell';
import { ISplashScreenProps } from '../../../interfaces/ISplashScreenProps';

import './SplashScreenShellMessage.css'
import { Spinner } from '../Spinner/Spinner';
import { IButtonLinkProps, ButtonLink } from '../ButtonLink/ButtonLink';

interface ISplashScreenMessageProps extends ISplashScreenProps {
    message: string;
    button?: IButtonLinkProps;
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
    return (
        <SplashScreenShell>
            <div className="vso-splash-screen__block">
                <div className="vso-splash-screen__message">
                    <MaybeSpinner {...props} /> { props.message }
                </div>
                <MaybeButton {...props} />
            </div>
        </SplashScreenShell>
    );
};
