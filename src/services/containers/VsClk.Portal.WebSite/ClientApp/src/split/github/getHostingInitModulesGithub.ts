
export const getHostingInitModules = async () => {
    return await Promise.all([
        import(/* webpackChunkName: "github-routes" */ './routesGithub'),
        import(/* webpackChunkName: "github-auth" */ './actions/getAuthToken'),
    ]);
};
