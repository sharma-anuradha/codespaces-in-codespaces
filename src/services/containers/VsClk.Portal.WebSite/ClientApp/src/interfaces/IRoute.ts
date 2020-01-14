import { RouteProps, RouteComponentProps } from 'react-router-dom';
import React from 'react';

export type IRoute = RouteProps & {
    authenticated: boolean;
    component: React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;
};
