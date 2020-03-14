import * as React from 'react';
import { useEffect } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { Workbench } from '../../../../components/workbench/workbench';
import { PageNotFoundGithub } from '../pageNotFound/pageNotFoundGithub';
import { FAVICON_PATH, DEFAULT_TITLE } from '../../constants';
import { WorkbenchSplashScreen } from '../../../../components/workbenchSplashScreen/workbenchSplashScreen';

interface IWorkbenchPageProps extends RouteComponentProps<{ id: string }> {}

export const WorkbenchPageGithub = (props: IWorkbenchPageProps) => {
    useEffect(() => {    
        document.title = DEFAULT_TITLE;
    });

    return (
        <Workbench
            connectingFavicon={FAVICON_PATH}
            workbenchFavicon={FAVICON_PATH}
            SplashScreenComponent={WorkbenchSplashScreen}
            PageNotFoundComponent={PageNotFoundGithub}
            title="Workspace"
            {...props}
        />
    );
}