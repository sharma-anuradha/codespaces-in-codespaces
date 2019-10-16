import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import { initializeIcons } from '@uifabric/icons';

import { App } from './app';
import { store } from './store/store';

import { trackUnhandled } from './utils/telemetry/unhandledErrors';

import './index.css';

function startApplication() {
    trackUnhandled();
    initializeIcons();

    const baseUrl = (document.getElementById('public_url') as HTMLBaseElement).getAttribute('href');
    const rootElement = document.getElementById('root');

    const enableTraceFactory = (traceName: string) => {
        return () => {
            localStorage.debug = traceName;
        };
    };

    const win = window as any;
    win.vsoEnablePortalTrace = enableTraceFactory('vsa-portal-webapp,vsa-portal-webapp:*');
    win.vsoEnableSshTrace = enableTraceFactory('vs-ssh,vs-ssh:*');

    ReactDOM.render(
        <BrowserRouter basename={baseUrl || ''}>
            <App store={store} />
        </BrowserRouter>,
        rootElement
    );
}

// Don't start application in iframe created by MSAL.
if (window.parent === window) {
    startApplication();
}
