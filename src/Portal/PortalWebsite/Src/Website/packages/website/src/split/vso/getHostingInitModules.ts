export const getHostingInitModules = async () => {
    return await Promise.all([
        import(/* webpackChunkName: "routes" */ './routes'),
        import(/* webpackChunkName: "auth" */ '../../actions/getAuthToken'),
        import(/* webpackChunkName: "auth-service" */ '../../services/authService'),
    ]);
};
