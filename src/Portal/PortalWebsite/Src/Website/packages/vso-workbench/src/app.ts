import * as React from 'react';
import * as ReactDom from 'react-dom';

import { updateFavicon } from 'vso-client-core';

import { telemetry } from './telemetry/telemetry';
import App from './react-app/app';

import { getVSCodeAssetPath } from './utils/getVSCodeAssetPath';
import { checkTemporaryGitHubIFrameHandshake } from './utils/temp__checkGitHubIFrameHandshake';
import { initAMDConfig } from './amdconfig';

updateFavicon(getVSCodeAssetPath('favicon.ico'));
initAMDConfig();
checkTemporaryGitHubIFrameHandshake();

telemetry.initializeTelemetry((_: string) => null);

const rootHtmlElement = document.querySelector('#js-vscode-workbench');
if (!rootHtmlElement) {
    throw new Error('No workbench DOM element found.');
}

ReactDom.render(React.createElement(App), rootHtmlElement);
