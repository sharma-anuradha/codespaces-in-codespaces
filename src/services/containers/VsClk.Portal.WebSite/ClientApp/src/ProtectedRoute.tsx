import React from 'react';
import { Route, Redirect, RouteComponentProps, RouteProps } from 'react-router-dom';
import { connect } from 'react-redux';

import { ApplicationState } from './reducers/rootReducer';
import { Loader } from './components/loader/loader';
import { telemetry } from './utils/telemetry';

const getAuthInfo = ({
    authentication: { isAuthenticated, isAuthenticating },
}: ApplicationState) => ({
    isAuthenticated,
    isAuthenticating,
});

type Props = {
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
    isAuthenticated: boolean;
    isAuthenticating: boolean;
} & RouteProps &
    RouteComponentProps;

const ProtectedRouteView = (props: Props) => {
    if (props.match.path === '/environment/:id') {
        telemetry.setCurrentEnvironmentId((props.match.params as any).id as string);
    } else {
        telemetry.setCurrentEnvironmentId(undefined);
    }

    const { isAuthenticating, isAuthenticated, component: Component, ...rest } = props;

    if (isAuthenticating && !isAuthenticated) {
        return <Loader message='Signing in...' />;
    }

    if (!isAuthenticating && !isAuthenticated) {
        return (
            <Redirect
                to={{
                    pathname: '/welcome',
                    search: new URLSearchParams({
                        redirectUrl: location.href.substr(location.origin.length),
                    }).toString(),
                }}
            />
        );
    }

    return <Component {...rest} />;
};

const AuthenticatedRoute = connect(getAuthInfo)(ProtectedRouteView);

type ProtectedRouteProps = {
    path: string;
    exact?: boolean;
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
};

export const ProtectedRoute = (props: ProtectedRouteProps) => {
    const { component, ...rest } = props;
    const render: RouteProps['render'] = (props: RouteComponentProps<any>) => (
        <AuthenticatedRoute {...props} component={component} />
    );
    return <Route {...rest} render={render} />;
};
