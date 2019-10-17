import React from 'react';

import { TitleBar } from '../titlebar/titlebar';
import { Navigation } from '../navigation/navigation';

import './portalLayout.css';

export function PortalLayout({
    children,
    hideNavigation,
}: React.PropsWithChildren<{ hideNavigation?: boolean }>) {
    const navigation = hideNavigation ? null : (
        <div className='ms-Grid-col ms-bgColor-gray20 portal-layout__app-navigation-container'>
            <Navigation />
        </div>
    );
    return (
        <div className='ms-Grid portal-layout'>
            <div className='ms-Grid-row'>
                <TitleBar />
            </div>
            <div className='ms-Grid-row portal-layout__app-content'>
                {navigation}
                <div className='ms-Grid-col portal-layout__app-content-container'>{children}</div>
            </div>
        </div>
    );
}
