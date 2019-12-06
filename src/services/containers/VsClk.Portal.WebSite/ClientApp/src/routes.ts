import { RouteProps, RouteComponentProps, matchPath as routerMatch, match } from 'react-router-dom';

import { NewEnvironment } from './components/newEnvironment/new-environment';
import { NewPlan } from './components/newPlan/new-plan';
import { EnvironmentsPanel } from './components/environments/environments';
import { Login } from './components/login/login';
import { Workbench } from './components/workbench/workbench';
import { LiveShareWorkbench } from './components/liveShareWorkbench/liveShareWorkbench';
import { GitHubLogin } from './components/gitHubLogin/gitHubLogin';
import { BlogPost } from './BlogPost';
import { PageNotFound } from './components/pageNotFound/pageNotFound';
import {
    environmentPath,
    environmentsPath,
    newEnvironmentPath,
    newPlanPath,
    githubLoginPath,
    loginPath,
    rootPath,
    pageNotFoundPath,
    liveShareSessionPath,
} from './routerPaths';

type Route = RouteProps & {
    authenticated: boolean;
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
};

export const routes: Route[] = [
    {
        authenticated: true,
        path: environmentPath,
        exact: false,
        component: Workbench,
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
        path: githubLoginPath,
        exact: false,
        component: GitHubLogin,
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

// Match path is for telemetry only.
export function matchPath(pathname: string): match<{}> | null {
    for (const route of routes) {
        const match = routerMatch(pathname, route);
        if (match) {
            return match;
        }
    }

    // if router match didnt happen routing user to page not found.
    return routerMatch(pathname, {
        path: pageNotFoundPath,
        component: PageNotFound,
    });
}
