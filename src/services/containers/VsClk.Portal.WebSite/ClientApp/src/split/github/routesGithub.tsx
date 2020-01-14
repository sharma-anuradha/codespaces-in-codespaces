
import { WorkbenchPageGithub } from './components/workbenchPage/workbenchPageGithub';
import { PageNotFoundGithub } from './components/pageNotFound/pageNotFoundGithub';
import { RouterConfig } from '../vso/routerConfig';
import { IRoute } from '../../interfaces/IRoute';

export const routes: IRoute[] = [
    {
        path: '/workspace/:id',
        exact: false,
        component: WorkbenchPageGithub,
        authenticated: false
    },
    {
        component: PageNotFoundGithub,
        authenticated: false
    },
];

export const routerConfig = new RouterConfig(routes);