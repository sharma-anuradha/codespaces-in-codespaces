import * as React from 'react';
import * as ReactDom from 'react-dom';

import { telemetry } from './telemetry/telemetry';
import App from './react-app/app';

import { getVSCodeAssetPath } from './utils/getVSCodeAssetPath';
import { checkTemporaryGitHubIFrameHandshake } from './utils/temp__checkGitHubIFrameHandshake';
import { updateFavicon } from 'vso-client-core';

const managementFavicon = 'favicon.ico';
const vscodeFavicon = getVSCodeAssetPath(managementFavicon);

checkTemporaryGitHubIFrameHandshake();

const el = document.querySelector('#js-vscode-workbench');
telemetry.initializeTelemetry((_: string) => null);

updateFavicon(vscodeFavicon);

if (!el) {
    throw new Error('No workbench DOM element found.');
}

ReactDom.render(React.createElement(App), el);
