import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';

import { App } from './app';
import { store } from './store/store';

import { sendTelemetry, telemetry, initTelemetry } from './utils/telemetry';
import { trackUnhandled } from './utils/telemetry/unhandledErrors';

import './index.css';
import { getHostingModules } from './getHostingInitModules';
import { initHostingHtmlTags } from './initHostingHtmlTags';

async function startApplication() {
    const [ hostingInitModules ] = await Promise.all([
        getHostingModules(),
        initHostingHtmlTags()
    ]);

    const { routeConfig, init } = hostingInitModules;
    const { matchPath, routes } = routeConfig;

    initTelemetry(matchPath);
    trackUnhandled();

    window.addEventListener('beforeunload', () => {
        sendTelemetry('vsonline/application/before-unload', {});
        telemetry.flush();
    });

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

    return ReactDOM.render(
        <BrowserRouter basename={baseUrl || ''}>
            <App
                store={store}
                init={init}
                routeConfig={routes}
            />
        </BrowserRouter>,
        rootElement
    );
}

// Don't start application in iframe created by MSAL.
if (window.parent === window) {
    startApplication();
}
