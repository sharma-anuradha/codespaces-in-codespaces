import * as React from 'react';
import * as ReactDom from 'react-dom';

import { updateFavicon } from 'vso-client-core';

import { telemetry } from './telemetry/telemetry';

import { initAMDConfig } from './amdconfig';
import { WorkbenchPage } from './react-app/components/WorkbenchPage/WorkbenchPage';
import { authService } from './auth/authService';

import './react-app/style/index.css';
import { getFaviconPath } from './utils/getFaviconPath';

initAMDConfig();

telemetry.initializeTelemetry((_: string) => null);

const rootHtmlElement = document.querySelector('#js-vscode-workbench');
if (!rootHtmlElement) {
    throw new Error('No workbench DOM element found.');
}

(async () => {
    const platformInfo = await authService.getPartnerInfo();
    updateFavicon(getFaviconPath(platformInfo));

    ReactDom.render(<WorkbenchPage platformInfo={platformInfo} />, rootHtmlElement);
})();
