import { RouteProps, RouteComponentProps, matchPath as routerMatch, match } from 'react-router-dom';

import { NewEnvironment } from './components/newEnvironment/newEnvironment';
import { EnvironmentsPanel } from './components/environments/environments';
import { Login } from './components/login/login';
import { Workbench } from './components/workbench/workbench';
import { GitHubLogin } from './components/gitHubLogin/gitHubLogin';
import { BlogPost } from './BlogPost';

type Route = RouteProps & {
    authenticated: boolean;
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
};

export const routes: Route[] = [
    {
        authenticated: true,
        path: '/environment/:id',
        exact: false,
        component: Workbench,
    },
    {
        authenticated: true,
        path: '/environments',
        exact: true,
        component: EnvironmentsPanel,
    },
    {
        authenticated: true,
        path: '/environments/new',
        exact: true,
        component: NewEnvironment,
    },
    {
        authenticated: true,
        path: '/github/login',
        exact: false,
        component: GitHubLogin,
    },
    {
        authenticated: false,
        path: '/login',
        exact: false,
        component: Login,
    },
    {
        authenticated: false,
        path: '/',
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
