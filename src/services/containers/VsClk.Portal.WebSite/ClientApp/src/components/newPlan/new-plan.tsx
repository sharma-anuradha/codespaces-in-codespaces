import React, { Fragment, useCallback } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { EnvironmentsPanel } from '../environments/environments';
import { CreatePlanPanel } from '../environmentsPanel/create-plan-panel';
import { environmentsPath } from '../../routes';

export function NewPlan(props: RouteComponentProps) {

    const hidePanel = useCallback(() => {
        // going back to environments cards (landing page)
        props.history.replace(environmentsPath);
    }, [props.history]);

    return (
        <Fragment>
            <EnvironmentsPanel {...props} />
            <CreatePlanPanel
                    hidePanel={hidePanel}
                />
        </Fragment>
    ); 
}
