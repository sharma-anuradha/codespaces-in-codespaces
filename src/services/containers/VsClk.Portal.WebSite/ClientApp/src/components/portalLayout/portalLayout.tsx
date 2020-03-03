import React from 'react';

import { TitleBar } from '../titlebar/titlebar';
import { Navigation } from '../navigation/navigation';

import './portalLayout.css';
import { isHostedOnGithub } from '../../utils/isHostedOnGithub';

const MaybeNavigation = (props: { hideNavigation?: boolean; }) => {
    if (props.hideNavigation) {
        return null;
    }

    return (
        <div className='ms-Grid-col ms-bgColor-gray20 portal-layout__app-navigation-container'>
            <Navigation />
        </div>
    );
};

const MaybeHeader = () => {
    if (isHostedOnGithub()) {
        return null;
    }

    return (
        <header className='ms-Grid-row'>
            <TitleBar />
        </header>
    );
};

export function PortalLayout({
    children,
    hideNavigation,
}: React.PropsWithChildren<{ hideNavigation?: boolean }>) {
    return (
        <div className='ms-Grid portal-layout'>
            <MaybeHeader />
            <main className='ms-Grid-row portal-layout__app-content'>
                <MaybeNavigation hideNavigation={hideNavigation} />
                <div className='ms-Grid-col portal-layout__app-content-container'>{children}</div>
            </main>
        </div>
    );
}
