
export const getHostingInitModules = async () => {
    return await Promise.all([
        import(/* webpackChunkName: "routes" */ './routes'),
        import(/* webpackChunkName: "auth" */ '../../actions/getAuthToken'),
    ]);
};
