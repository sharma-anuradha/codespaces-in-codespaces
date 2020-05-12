import { isHostedOnGithub } from 'vso-client-core';

import { init } from './actions/init';

import { getHostingInitModules as getGithubModules } from './split/github/getHostingInitModulesGithub';
import { getHostingInitModules as getVSOModules } from './split/vso/getHostingInitModules';
import { AuthService } from './services/authService';
import { AuthServiceGithub } from './split/github/authServiceGithub';
import { RouterConfig as GitHubRouterConfig } from './split/github/routesGithub';
import { RouterConfig as VsoRouterConfig } from './split/vso/routerConfig';

export type HostingModules = {
    routeConfig: VsoRouterConfig | GitHubRouterConfig,
    init: any,
    authService: AuthServiceGithub | AuthService
}

export const getHostingModules: () => Promise<HostingModules> = async () => {
    const [routesModule, getAuthTokenModule, authServiceModule] = !isHostedOnGithub()
        ? await getVSOModules()
        : await getGithubModules();

    const routeConfig = routesModule.routerConfig;

    return {
        routeConfig,
        init: init.bind(null, getAuthTokenModule.getAuthToken),
        authService: authServiceModule.authService,
    };
};
