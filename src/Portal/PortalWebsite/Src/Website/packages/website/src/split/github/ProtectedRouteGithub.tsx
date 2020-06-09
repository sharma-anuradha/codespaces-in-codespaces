import React from 'react';
import { Route, RouteComponentProps, RouteProps } from 'react-router-dom';
import { connect } from 'react-redux';

import { createTrace } from 'vso-client-core';

import { telemetry } from '../../utils/telemetry';
import { authService } from './authServiceGithub';
import { ApplicationState } from '../../reducers/rootReducer';
import { Loader } from '../../components/loader/loader';
import { useTranslation } from 'react-i18next';

const trace = createTrace('protected-route');

type Props = {
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
    isAuthenticated: boolean;
    isAuthenticating: boolean;
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
        isAuthenticating,
        isAuthenticated,
        component: Component,
        ...rest
    } = props;

    if (isAuthenticating && !isAuthenticated) {
        return <Loader translation={translation} />;
    }

    if (!isAuthenticating && !isAuthenticated) {
        authService.getCascadeToken().catch((e) => {
            trace.warn(e);
        });

        return null;
    }

    return <Component {...rest} />;
};

const getAccessInfo = ({
    authentication: { isAuthenticated, isAuthenticating },
}: ApplicationState) => ({
    isAuthenticated,
    isAuthenticating,
});

const AuthenticatedRoute = connect(getAccessInfo)(ProtectedRouteView);


type ProtectedRouteProps = RouteProps & {
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
};

export const ProtectedRouteGithub = (props: ProtectedRouteProps) => {
    const { component, ...rest } = props;
    const render: RouteProps['render'] = (props: RouteComponentProps<any>) => (
        <AuthenticatedRoute {...props} component={component} />
    );
    return <Route {...rest} render={render} />;
};
