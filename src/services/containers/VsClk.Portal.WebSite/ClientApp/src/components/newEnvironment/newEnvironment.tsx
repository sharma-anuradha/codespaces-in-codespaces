import React, { Fragment, useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { createEnvironment } from '../../actions/createEnvironment';
import { EnvironmentsPanel } from '../environments/environments';
import { CreateEnvironmentPanel } from '../environmentsPanel/create-environment-panel';

export function NewEnvironment(props: RouteComponentProps) {
    const query = new URLSearchParams(props.location.search);
    const name = query.get('name');
    const repo = query.get('repo');

    const hidePanel = useCallback(() => {
        // going back to environments cards (landing page)
        props.history.replace('/environments');
    }, [props.history]);

    const dispatch = useDispatch();

    const createEnvironmentCallback = useCallback(
        (friendlyName: string, gitRepositoryUrl?: string) => {
            dispatch(
                createEnvironment({
                    friendlyName,
                    gitRepositoryUrl,
                })
            );

            hidePanel();
        },
        [dispatch, hidePanel]
    );

    return (
        <Fragment>
            <EnvironmentsPanel {...props} />
            <CreateEnvironmentPanel
                hidePanel={hidePanel}
                onCreateEnvironment={createEnvironmentCallback}
                defaultName={name}
                defaultRepo={repo}
            />
        </Fragment>
    );
}
