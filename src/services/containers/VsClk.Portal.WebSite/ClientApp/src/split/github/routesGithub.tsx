
import { matchPath as routerMatch, match } from 'react-router-dom';

import { WorkbenchPageGithub } from './components/workbenchPage/workbenchPageGithub';
import { PageNotFoundGithub } from './components/pageNotFound/pageNotFoundGithub';
import { IRoute } from '../../interfaces/IRoute';
import { Route as BrowserRoute } from 'react-router-dom';
import React from 'react';
import { GitHubLogin } from '../../components/gitHubLogin/gitHubLogin';
import { ProtectedRouteGithub } from './ProtectedRouteGithub';

export const pageNotFoundPath = '/pageNotFound';

export class RouterConfig {
    constructor(
        private rawRoutes: IRoute[]
    ) {}

    matchPath(pathname: string): match<{}> | null {
        for (const route of routes) {
            const match = routerMatch(pathname, route);
            if (match) {
                return match;
            }
        }
        // if router match didnt happen routing user to page not found.
        return routerMatch(pathname, {
            path: pageNotFoundPath,
            component: PageNotFoundGithub,
        });
    }

    public get routes() {
        const routeConfig = this.rawRoutes.map((r, i) => {
            const { ...props } = r;
            if (r.authenticated) {
                return <ProtectedRouteGithub {...props} key={i} />
            }
            return <BrowserRoute {...props} key={i} />;
        });
        return routeConfig;
    }
}

export const githubLoginPath = '/github/login';
export const environmentPath = '/environment/:id';

export const routes: IRoute[] = [
    {
        path: environmentPath,
        exact: false,
        component: WorkbenchPageGithub,
        authenticated: true
    },
    {
        authenticated: false,
        path: githubLoginPath,
        exact: false,
        component: GitHubLogin,
    },
    {
        component: PageNotFoundGithub,
        authenticated: false
    },
];

export const routerConfig = new RouterConfig(routes);