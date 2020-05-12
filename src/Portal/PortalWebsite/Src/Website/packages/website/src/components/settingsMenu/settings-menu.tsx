import React, { Fragment, useCallback } from 'react';
import { RouteComponentProps } from 'react-router-dom';
import { useSelector } from 'react-redux';

import { EnvironmentsPanel, getPlanEnvironments } from '../environments/environments';
import { SettingsMenuPanel } from './settings-menu-panel';

import { ApplicationState } from '../../reducers/rootReducer';

export function SettingsMenu(props: RouteComponentProps) {

    const { selectedPlan } = useSelector(
        (state: ApplicationState) => ({
            selectedPlan: state.plans.selectedPlan,
        })
    );

    const { environments } = useSelector(
        (state: ApplicationState) => ({
            environments: state.environments.environments
        })
    )

    const hidePanel = useCallback(() => {
        // going back to environments cards (landing page)
        props.history.replace('/environments');
    }, [props.history]);

    return (
        <Fragment>
            <EnvironmentsPanel {...props} />
            <SettingsMenuPanel 
                selectedPlan={selectedPlan} 
                environmentsInPlan={getPlanEnvironments(selectedPlan, environments)}
                hidePanel={hidePanel}
                {...props} 
            />
        </Fragment>
    );
}