import * as React from 'react';
import * as ReactDom from 'react-dom';

import { updateFavicon } from 'vso-client-core';

import { telemetry } from './telemetry/telemetry';

import { initAMDConfig } from './amdconfig';
import { WorkbenchPage } from './react-app/components/WorkbenchPage/WorkbenchPage';
import { authService } from './auth/authService';
import { getFaviconPath } from './utils/getFaviconPath';

import { initializeCodespacePerformanceInstance } from './utils/performance/CodespacePerformance';

import './react-app/style/index.css';
import { PerformanceEventIds, PerformanceEventNames } from './utils/performance/PerformanceEvents';

initAMDConfig();
telemetry.initializeTelemetry((_: string) => null);
const codespacePerformance = initializeCodespacePerformanceInstance();

const rootHtmlElement = document.querySelector('#js-vscode-workbench');
if (!rootHtmlElement) {
    throw new Error('No workbench DOM element found.');
}

(async () => {
    const platformInfo = await codespacePerformance.measure(
        { name: PerformanceEventNames.InitGetPlatformInfo },
        async () => {
            return await authService.getPartnerInfo();
        }
    );

    await codespacePerformance.measure(
        { name: PerformanceEventNames.InitUpdateFavicon },
        async () => {
            return updateFavicon(getFaviconPath(platformInfo));
        }
    );

    codespacePerformance.markBlockStart({
        id: PerformanceEventIds.InitTimeToRemoteExtensions,
        name: 'time to terminal/remote extensions',
    });

    ReactDom.render(
        <WorkbenchPage
            platformInfo={platformInfo}
            performance={codespacePerformance} />,
        rootHtmlElement);
})();
