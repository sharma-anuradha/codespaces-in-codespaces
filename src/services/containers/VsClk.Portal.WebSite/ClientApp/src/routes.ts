import { RouteProps, RouteComponentProps, matchPath as routerMatch, match } from 'react-router-dom';

import { NewEnvironment } from './components/newEnvironment/new-environment';
import { NewAccount } from './components/newAccount/new-account';
import { EnvironmentsPanel } from './components/environments/environments';
import { Login } from './components/login/login';
import { Workbench } from './components/workbench/workbench';
import { GitHubLogin } from './components/gitHubLogin/gitHubLogin';
import { BlogPost } from './BlogPost';

type Route = RouteProps & {
    authenticated: boolean;
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
};

export const rootPath = '/';
export const environmentPath = '/environment/:id';
export const environmentsPath = '/environments';
export const newEnvironmentPath = '/environments/new';
export const loginPath = '/login';
export const githubLoginPath = '/github/login';
export const newAccountPath = '/environments/plan';

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
        path: newAccountPath,
        exact: true,
        component: NewAccount,
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
        path: rootPath,
        exact: true,
        component: BlogPost,
    },
];

export function matchPath(pathname: string): match<{}> | null {
    for (const route of routes) {
        const match = routerMatch(pathname, route);
        if (match) {
            return match;
        }
    }

    return null;
}
