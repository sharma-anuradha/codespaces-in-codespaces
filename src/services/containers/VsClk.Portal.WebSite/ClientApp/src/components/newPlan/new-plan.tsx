import React, { Fragment, useCallback } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { EnvironmentsPanel } from '../environments/environments';
import { CreatePlanPanel } from '../environmentsPanel/create-plan-panel';
import { environmentsPath, newEnvironmentPath } from '../../routes';

export function NewPlan(props: RouteComponentProps) {
    const hidePanel = useCallback(() => {
        const query = new URLSearchParams(props.location.search);

        const newPath = query.get('type') === 'wizard' ? newEnvironmentPath : environmentsPath;

        props.history.replace(newPath);
    }, [props.history, props.location]);

    return (
        <Fragment>
            <EnvironmentsPanel {...props} />
            <CreatePlanPanel hidePanel={hidePanel} />
        </Fragment>
    );
}
