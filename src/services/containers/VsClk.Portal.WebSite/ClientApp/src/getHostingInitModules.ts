import { isHostedOnGithub } from './utils/isHostedOnGithub';
import { init } from './actions/init';

import { getHostingInitModules as getGithubModules } from './split/github/getHostingInitModulesGithub';
import { getHostingInitModules as getVSOModules } from './split/vso/getHostingInitModules';

export const getHostingModules = async () => {
    const [routesModule, getAuthTokenModule, authServiceModule] = !isHostedOnGithub()
        ? await getVSOModules()
        : await getGithubModules();

    const routeConfig = await routesModule.routerConfig;

    return {
        routeConfig,
        init: init.bind(null, getAuthTokenModule.getAuthToken),
        authService: authServiceModule.authService,
    };
};
