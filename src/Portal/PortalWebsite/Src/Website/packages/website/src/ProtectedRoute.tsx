import React from 'react';
import { Route, Redirect, RouteComponentProps, RouteProps } from 'react-router-dom';
import { connect } from 'react-redux';

import { ApplicationState } from './reducers/rootReducer';
import { Loader } from './components/loader/loader';
import { telemetry } from './utils/telemetry';
import { loginPath, environmentsPath } from './routerPaths';
import { ServiceUnavailable } from './components/ServiceUnavailable/ServiceUnavailable';
import { useTranslation } from 'react-i18next';

type Props = {
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
    isAuthenticated: boolean;
    isAuthenticating: boolean;
    isServiceAvailable: boolean;
} & RouteProps &
    RouteComponentProps;

const ProtectedRouteView = (props: Props) => {
    const { t: translation } = useTranslation();
    if (props.match.path === '/environment/:id') {
        telemetry.setCurrentEnvironmentId((props.match.params as any).id as string);
    } else {
        telemetry.setCurrentEnvironmentId(undefined);
    }

    const {
        isServiceAvailable,
        isAuthenticating,
        isAuthenticated,
        component: Component,
        ...rest
    } = props;

    if (!isServiceAvailable) {
        return <ServiceUnavailable />;
    }

    if (isAuthenticating && !isAuthenticated) {
        return <Loader message={translation('signingIn')} translation={translation}/>;
    }

    if (!isAuthenticating && !isAuthenticated) {
        const search =
            location.pathname === environmentsPath
                ? undefined
                : new URLSearchParams({
                      redirectUrl: location.href,
                  }).toString();
        return (
            <Redirect
                to={{
                    pathname: loginPath,
                    search,
                }}
            />
        );
    }

    return <Component {...rest} />;
};

const getAccessInfo = ({
    authentication: { isAuthenticated, isAuthenticating },
    serviceStatus: { isServiceAvailable },
}: ApplicationState) => ({
    isAuthenticated,
    isAuthenticating,
    isServiceAvailable,
});

const AuthenticatedRoute = connect(getAccessInfo)(ProtectedRouteView);

type ProtectedRouteProps = RouteProps & {
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
};

export const ProtectedRoute = (props: ProtectedRouteProps) => {
    const { component, ...rest } = props;
    const render: RouteProps['render'] = (props: RouteComponentProps<any>) => (
        <AuthenticatedRoute {...props} component={component} />
    );
    return <Route {...rest} render={render} />;
};
