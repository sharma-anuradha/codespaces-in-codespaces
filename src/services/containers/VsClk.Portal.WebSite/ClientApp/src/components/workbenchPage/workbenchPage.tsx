import * as React from 'react';
import { Component } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { Workbench } from '../workbench/workbench';
import { WorkbenchSplashScreen } from '../workbenchSplashScreen/workbenchSplashScreen';
import { PageNotFound } from '../pageNotFound/pageNotFound';

interface IWorkbenchPageProps extends RouteComponentProps<{ id: string }> {}

export class WorkbenchPage extends Component<IWorkbenchPageProps, {}> {
    render() {
        return (
            <Workbench
                connectingFavicon={'favicon.ico'}
                workbenchFavicon={'static/web-standalone/favicon.ico'}
                SplashScreenComponent={WorkbenchSplashScreen}
                PageNotFoundComponent={PageNotFound}
                {...this.props}
            />
        );
    }
}