import React, { useEffect, Fragment } from 'react';
import { useSelector } from 'react-redux';
import { Redirect } from 'react-router-dom';
import { ApplicationState } from './reducers/rootReducer';
import { loginPath, environmentsPath } from './routerPaths';
import { RouteComponentProps } from 'react-router';
import { blogPostUrl } from './constants';

import { isInsidePopupWindow } from './utils/isInsidePopupWindow';

const blogPostSeenLocaltorageKey = 'vso.marketing.blog.post.seen';

export function BlogPost(props: RouteComponentProps) {
    const blogPostKey = localStorage.getItem(blogPostSeenLocaltorageKey);

    if ((process.env.NODE_ENV === 'development') || isInsidePopupWindow() || blogPostKey) {
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

        localStorage.setItem(blogPostSeenLocaltorageKey, 'true');
        
        window.location.replace(blogPostUrl);
    });

    return <Fragment />;
}
