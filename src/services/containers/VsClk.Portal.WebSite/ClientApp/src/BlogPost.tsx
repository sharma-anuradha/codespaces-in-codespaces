import React, { useEffect, Fragment } from 'react';
import { useSelector } from 'react-redux';
import { Redirect } from 'react-router-dom';
import { ApplicationState } from './reducers/rootReducer';

export function BlogPost() {
    if (process.env.NODE_ENV === 'development') {
        return <Redirect to='/login' />;
    }

    const isAuthenticated = useSelector(
        (state: ApplicationState) => state.authentication.isAuthenticated
    );

    useEffect(() => {
        if (isAuthenticated) {
            return;
        }

        window.location.replace(
            'https://devblogs.microsoft.com/visualstudio/intelligent-productivity-and-collaboration-from-anywhere/'
        );
    });

    return <Fragment />;
}
