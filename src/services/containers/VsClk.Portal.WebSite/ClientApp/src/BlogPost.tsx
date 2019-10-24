import React, { useEffect, Fragment } from 'react';
import { useSelector } from 'react-redux';
import { Redirect } from 'react-router-dom';
import { ApplicationState } from './reducers/rootReducer';
import { loginPath, environmentsPath } from './routes';
import { RouteComponentProps } from 'react-router';

export const blogPostUrl =
    'https://devblogs.microsoft.com/visualstudio/intelligent-productivity-and-collaboration-from-anywhere/';

export function BlogPost(props: RouteComponentProps) {
    if (process.env.NODE_ENV === 'development') {
        return <Redirect to={loginPath} />;
    }

    const isAuthenticated = useSelector(
        (state: ApplicationState) => state.authentication.isAuthenticated
    );

    useEffect(() => {
        if (isAuthenticated) {
            props.history.push(environmentsPath);
            return;
        }

        window.location.replace(blogPostUrl);
    });

    return <Fragment />;
}
