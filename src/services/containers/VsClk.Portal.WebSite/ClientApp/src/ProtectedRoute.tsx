import React from 'react';
import { Route, Redirect, RouteComponentProps, RouteProps, withRouter } from 'react-router-dom';
import { connect } from 'react-redux';

import { ApplicationState } from './reducers/rootReducer';
import { Loader } from './components/loader/loader';

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
    const { isAuthenticating, isAuthenticated, component: Component, ...rest } = props;

    if (isAuthenticating && !isAuthenticated) {
        return <Loader message='Signing in...' />;
    }

    if (!isAuthenticating && !isAuthenticated) {
        return <Redirect to='/welcome' />;
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
