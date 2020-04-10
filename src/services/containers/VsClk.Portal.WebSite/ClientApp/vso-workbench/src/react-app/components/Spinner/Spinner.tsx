import * as React from 'react';

import { ISplashScreenProps } from '../../../interfaces/ISplashScreenProps';

import './Spinner.css';

interface ISpinnerProps extends ISplashScreenProps {
    isStopped?: boolean;
}

export const Spinner: React.FunctionComponent<ISpinnerProps> = (props: ISpinnerProps) => {
    const animationClassName = props.isStopped
        ? 'vso-ms-spinner--stopped'
        : '';

    const className = `${animationClassName} ${props.className || ''}`;

    return (
        <div className={`vso-ms-spinner ${className}`}>
            <div className='vso-ms-spinner__rect vso-ms-spinner__rect--1'></div>
            <div className='vso-ms-spinner__rect vso-ms-spinner__rect--2'></div>
            <div className='vso-ms-spinner__rect vso-ms-spinner__rect--3'></div>
            <div className='vso-ms-spinner__rect vso-ms-spinner__rect--4'></div>
        </div>
    );
};
