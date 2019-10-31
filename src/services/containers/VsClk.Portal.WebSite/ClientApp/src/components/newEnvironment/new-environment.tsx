import React, { Fragment, useCallback, useState } from 'react';
import { useDispatch } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { createEnvironment } from '../../actions/createEnvironment';
import { EnvironmentsPanel } from '../environments/environments';
import {
    CreateEnvironmentPanel,
    defaultAutoShutdownDelayMinutes,
} from '../environmentsPanel/create-environment-panel';

type CreateEnvironmentParams = Parameters<typeof createEnvironment>[0];

export function NewEnvironment(props: RouteComponentProps) {
    const query = new URLSearchParams(props.location.search);
    const name = query.get('name');
    const repo = query.get('repo');
    const skuName = query.get('instanceType');

    const hidePanel = useCallback(() => {
        // going back to environments cards (landing page)
        props.history.replace('/environments');
    }, [props.history]);

    const [errorMessage, setErrorMessage] = useState(undefined as undefined | string);

    const dispatch = useDispatch();
    const createEnvironmentCallback = useCallback(
        async (parameters: CreateEnvironmentParams) => {
            try {
                await dispatch(createEnvironment(parameters));

                storeDotfilesConfiguration(parameters);

                hidePanel();
            } catch (err) {
                setErrorMessage(err.message);
            }
        },
        [hidePanel]
    );

    const hideError = useCallback(() => setErrorMessage(undefined), []);

    return (
        <Fragment>
            <EnvironmentsPanel {...props} />
            <CreateEnvironmentPanel
                {...getStoredDotfilesConfiguration()}
                defaultName={name}
                defaultRepo={repo}
                defaultSkuName={skuName}
                hidePanel={hidePanel}
                errorMessage={errorMessage}
                hideErrorMessage={hideError}
                onCreateEnvironment={createEnvironmentCallback}
                autoShutdownDelayMinutes={defaultAutoShutdownDelayMinutes}
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
        } else {
            localStorage.removeItem(dotfilesInstallCommandLocalStorageKey);
        }

        if (dotfilesRepository) {
            localStorage.setItem(dotfilesRepositoryLocalStorageKey, dotfilesRepository);
        } else {
            localStorage.removeItem(dotfilesRepositoryLocalStorageKey);
        }

        if (dotfilesTargetPath) {
            localStorage.setItem(dotfilesTargetPathLocalStorageKey, dotfilesTargetPath);
        } else {
            localStorage.removeItem(dotfilesTargetPathLocalStorageKey);
        }
    } catch (err) {
        // noop
    }
}
