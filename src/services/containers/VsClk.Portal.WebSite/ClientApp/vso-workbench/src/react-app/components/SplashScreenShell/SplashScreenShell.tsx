import * as React from 'react';

import './SplashScreenShell.css';
import { ISplashScreenProps } from '../../../interfaces/ISplashScreenProps';

export const SplashScreenShell: React.FunctionComponent<ISplashScreenProps> = (props: ISplashScreenProps) => {
    const {
        children,
        className
    } = props;

    return (<div className={`vsonline-splash-screen-main ${className || ''}`}>
        <div className="vsonline-splash-screen-extensions-pane"></div>
        <div className='vsonline-splash-screen-tree-pane'></div>
        <div className="vsonline-splash-screen-editor">
            <div className='vsonline-splash-screen-titlebar'>
                <div className='vsonline-splash-screen-titlebar-tab'></div>
            </div>
            <div className="vsonline-splash-screen-body">
                {children}
            </div>
        </div>
    </div>);
};
