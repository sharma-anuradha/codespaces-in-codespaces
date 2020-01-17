import React, { Fragment, useCallback } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { EnvironmentsPanel } from '../environments/environments';
import { CreatePlanPanel } from '../environmentsPanel/create-plan-panel';
import { environmentsPath, newEnvironmentPath } from '../../routerPaths';
import { focusPlanSelectorDropdown } from '../../actions/plans-actions';

export function NewPlan(props: RouteComponentProps) {
    const hidePanel = useCallback(
        (canContinueToEnvironment = false) => {
            const query = new URLSearchParams(props.location.search);

            const isWizard = query.get('type') === 'wizard';

            const newPath =
                isWizard && canContinueToEnvironment ? newEnvironmentPath : environmentsPath;

            focusPlanSelectorDropdown();

            props.history.replace(newPath);
        },
        [props.history, props.location]
    );

    return (
        <Fragment>
            <EnvironmentsPanel {...props} />
            <CreatePlanPanel hidePanel={hidePanel} />
        </Fragment>
    );
}
