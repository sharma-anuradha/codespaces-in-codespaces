import { matchPath as routerMatch, match } from 'react-router-dom';
import React from 'react';
import { Route as BrowserRoute } from 'react-router-dom';
import { PageNotFound } from '../../components/pageNotFound/pageNotFound';
import { pageNotFoundPath } from '../../routerPaths';
import { ProtectedRoute } from '../../ProtectedRoute';
import { routes } from './routes';

import { IRoute } from "../../interfaces/IRoute";

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
            component: PageNotFound,
        });
    }

    public get routes() {
        const routeConfig = this.rawRoutes.map((r, i) => {
            const { authenticated, ...props } = r;
            return authenticated ? (<ProtectedRoute {...props} key={i} />) : (<BrowserRoute {...props} key={i} />);
        });
        return routeConfig;
    }
}
