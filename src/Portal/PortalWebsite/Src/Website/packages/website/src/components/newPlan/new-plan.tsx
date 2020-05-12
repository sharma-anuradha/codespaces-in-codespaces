import React, { Fragment, useCallback } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { EnvironmentsPanel } from '../environments/environments';
import { CreatePlanPanel } from '../environmentsPanel/create-plan-panel';
import { environmentsPath, newEnvironmentPath } from '../../routerPaths';
import { focusPlanSelectorDropdown } from '../../actions/plans-actions';

export function NewPlan(props: RouteComponentProps) {
    const hidePanel = useCallback(
        (canContinue = false) => {
            const query = new URLSearchParams(props.location.search);
            const redirectUrl = query.get('redirectUrl');
            const showCreateEnvironmentPanel = query.get('showCreateEnvironmentPanel') === 'true';
            const nextPath =
                showCreateEnvironmentPanel && canContinue ? newEnvironmentPath : environmentsPath;

            focusPlanSelectorDropdown();

            props.history.push({
                pathname: nextPath,
                search: redirectUrl ? redirectUrl.toString() : '',
            });
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
