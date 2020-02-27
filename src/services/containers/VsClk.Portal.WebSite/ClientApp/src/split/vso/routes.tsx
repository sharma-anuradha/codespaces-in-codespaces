
import { NewEnvironment } from '../../components/newEnvironment/new-environment';
import { NewPlan } from '../../components/newPlan/new-plan';
import { EnvironmentsPanel } from '../../components/environments/environments';
import { Login } from '../../components/login/login';
import { WorkbenchPage } from '../../components/workbenchPage/workbenchPage';
import { LiveShareWorkbench } from '../../components/liveShareWorkbench/liveShareWorkbench';
import { GitHubWorkbench } from '../../components/gitHubWorkbench/gitHubWorkbench';
import { GistWorkbench } from '../../components/gistWorkbench/gistWorkbench';
import { GitHubLogin } from '../../components/gitHubLogin/gitHubLogin';
import { AzDevLogin } from '../../components/gitHubLogin/azDevLogin';
import { BlogPost } from '../../BlogPost';
import { PageNotFound } from '../../components/pageNotFound/pageNotFound';
import { SettingsMenu } from '../../components/settingsMenu/settings-menu';
import {
    environmentPath,
    environmentsPath,
    newEnvironmentPath,
    newPlanPath,
    settingsPath,
    githubLoginPath,
    azdevLoginPath,
    loginPath,
    rootPath,
    liveShareSessionPath,
    githubPath,
    gistPath,
} from '../../routerPaths';

import { IRoute } from '../../interfaces/IRoute';
import { RouterConfig } from './routerConfig';

export const routes: IRoute[] = [
    {
        authenticated: true,
        path: environmentPath,
        exact: false,
        component: WorkbenchPage,
    },
    {
        authenticated: true,
        path: environmentsPath,
        exact: true,
        component: EnvironmentsPanel,
    },
    {
        authenticated: true,
        path: newEnvironmentPath,
        exact: true,
        component: NewEnvironment,
    },
    {
        authenticated: true,
        path: newPlanPath,
        exact: true,
        component: NewPlan,
    },
    {
        authenticated: true,
        path: settingsPath,
        exact: true,
        component: SettingsMenu,
    },
    {
        authenticated: true,
        path: githubLoginPath,
        exact: false,
        component: GitHubLogin,
    },
    {
        authenticated: true,
        path: azdevLoginPath,
        exact: false,
        component: AzDevLogin,
    },
    {
        authenticated: false,
        path: loginPath,
        exact: false,
        component: Login,
    },
    {
        authenticated: false,
        path: liveShareSessionPath,
        exact: false,
        component: LiveShareWorkbench,
    },
    {
        authenticated: true,
        path: githubPath,
        exact: false,
        component: GitHubWorkbench,
    },
    {
        authenticated: false,
        path: gistPath,
        exact: false,
        component: GistWorkbench,
    },
    {
        authenticated: false,
        path: rootPath,
        exact: true,
        component: BlogPost,
    },
    {
        authenticated: false,
        component: PageNotFound,
    },
];

export const routerConfig = new RouterConfig(routes);
