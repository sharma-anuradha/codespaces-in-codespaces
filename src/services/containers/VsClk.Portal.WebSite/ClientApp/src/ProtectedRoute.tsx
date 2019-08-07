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

export const ProtectedRoute = connect(getAuthInfo)(
    withRouter(({ component: Component, isAuthenticating, isAuthenticated, ...rest }: Props) => {
        const routeRender = (props: any) => {
            if (isAuthenticating && !isAuthenticated) {
                return <Loader message='Signing in...' />;
            }

            if (isAuthenticated) {
                return <Component {...props} />;
            }

            return <Redirect to='/' />;
        };

        return <Route {...rest} render={routeRender} />;
    })
);
