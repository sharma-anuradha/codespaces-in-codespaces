import * as React from 'react';
import { Component } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { getVSCodeAssetPath } from 'vso-workbench';

import { Workbench } from '../workbench/workbench';
import { WorkbenchSplashScreen } from '../workbenchSplashScreen/workbenchSplashScreen';
import { ServerlessSplashscreen } from 'vso-workbench';
import { PageNotFound } from '../pageNotFound/pageNotFound';

const managementFavicon = 'favicon.ico';
const vscodeFavicon = getVSCodeAssetPath('favicon.ico');

interface IWorkbenchPageProps extends RouteComponentProps<{ id: string }> {}

export class WorkbenchPage extends Component<IWorkbenchPageProps, {}> {
    render() {
        return (
            <Workbench
                connectingFavicon={managementFavicon}
                workbenchFavicon={vscodeFavicon}
                SplashScreenComponent={WorkbenchSplashScreen}
                ServerlessSplashscreenComponent={ServerlessSplashscreen}
                PageNotFoundComponent={PageNotFound}
                {...this.props}
            />
        );
    }
}
