import React, { Fragment, useCallback, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { createEnvironment, focusCreateEnvironmentButton } from '../../actions/createEnvironment';
import { EnvironmentsPanel } from '../environments/environments';
import {
    CreateEnvironmentPanel,
    defaultAutoShutdownDelayMinutes,
} from '../environmentsPanel/create-environment-panel';
import { ApplicationState } from '../../reducers/rootReducer';
import { Loader } from '../loader/loader';
import { newPlanPath } from '../../routerPaths';

type CreateEnvironmentParams = Parameters<typeof createEnvironment>[0];

export function NewEnvironment(props: RouteComponentProps) {
    const { selectedPlan, isLoadingPlan, isMadeInitialPlansRequest } = useSelector(
        (state: ApplicationState) => ({
            selectedPlan: state.plans.selectedPlan,
            isLoadingPlan: state.plans.isLoadingPlan,
            isMadeInitialPlansRequest: state.plans.isMadeInitialPlansRequest,
        })
    );

    const query = new URLSearchParams(props.location.search);
    const redirectUrl = query && query.get('redirectUrl');
    const urlParams = redirectUrl ? new URLSearchParams(redirectUrl) : query;
    const name = urlParams.get('name');
    const repo = urlParams.get('repo');
    const skuName = urlParams.get('instanceType');

    const hidePanel = useCallback(
        (environmentId?: string) => {
            focusCreateEnvironmentButton();

            // going back to environments cards (landing page)
            if (environmentId) {
                window.location.replace(`/environment/${environmentId}`);
            } else {
                props.history.replace('/environments');
            }
        },
        [props.history]
    );

    const [errorMessage, setErrorMessage] = useState(undefined as undefined | string);

    const dispatch = useDispatch();
    const createEnvironmentCallback = useCallback(
        async (parameters: CreateEnvironmentParams) => {
            try {
                const environmentId = await dispatch(createEnvironment(parameters));

                storeDotfilesConfiguration(parameters);

                hidePanel(environmentId);
            } catch (err) {
                setErrorMessage(err.message);
            }
        },
        [hidePanel]
    );

    const hideError = useCallback(() => setErrorMessage(undefined), []);

    if (!isMadeInitialPlansRequest || isLoadingPlan) {
        return <Loader message='Fetching your billing plans...' />;
    }

    if (!selectedPlan) {
        let search = new URLSearchParams({
            showCreateEnvironmentPanel: 'true',
        });

        if (location.search) {
            search.append('redirectUrl', location.search);
        }

        props.history.push({
            pathname: newPlanPath,
            search: search.toString(),
        });
    }

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
