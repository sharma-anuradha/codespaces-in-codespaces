import React, { Fragment, useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { createEnvironment } from '../../actions/createEnvironment';
import { EnvironmentsPanel } from '../environments/environments';
import { CreateEnvironmentPanel } from '../environmentsPanel/create-environment-panel';

type CreateEnvironmentParams = Parameters<typeof createEnvironment>[0];

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
        (parameters: CreateEnvironmentParams) => {
            dispatch(createEnvironment(parameters));

            storeDotfilesConfiguration(parameters);

            hidePanel();
        },
        [dispatch, hidePanel]
    );

    return (
        <Fragment>
            <EnvironmentsPanel {...props} />
            <CreateEnvironmentPanel
                {...getStoredDotfilesConfiguration()}
                defaultName={name}
                defaultRepo={repo}
                hidePanel={hidePanel}
                onCreateEnvironment={createEnvironmentCallback}
            />
        </Fragment>
    );
}

const dotfilesInstallCommandLocalStorageKey = 'user_setting_dotfilesInstallCommand';
const dotfilesRepositoryLocalStorageKey = 'user_setting_dotfilesRepository';
const dotfilesTargetPathLocalStorageKey = 'user_setting_dotfilesTargetPath';

function getStoredDotfilesConfiguration(): {
    defaultDotfilesInstallCommand?: string | null;
    defaultDotfilesRepository?: string | null;
    defaultDotfilesTarget?: string | null;
} {
    const defaultDotfilesInstallCommand = localStorage.getItem(
        dotfilesInstallCommandLocalStorageKey
    );
    const defaultDotfilesRepository = localStorage.getItem(dotfilesRepositoryLocalStorageKey);
    const defaultDotfilesTarget = localStorage.getItem(dotfilesTargetPathLocalStorageKey);

    return { defaultDotfilesInstallCommand, defaultDotfilesRepository, defaultDotfilesTarget };
}

function storeDotfilesConfiguration({
    dotfilesInstallCommand,
    dotfilesRepository,
    dotfilesTargetPath,
}: CreateEnvironmentParams) {
    try {
        if (dotfilesInstallCommand) {
            localStorage.setItem(dotfilesInstallCommandLocalStorageKey, dotfilesInstallCommand);
        }
        if (dotfilesRepository) {
            localStorage.setItem(dotfilesRepositoryLocalStorageKey, dotfilesRepository);
        }
        if (dotfilesTargetPath) {
            localStorage.setItem(dotfilesTargetPathLocalStorageKey, dotfilesTargetPath);
        }
    } catch (err) {
        // noop
    }
}
