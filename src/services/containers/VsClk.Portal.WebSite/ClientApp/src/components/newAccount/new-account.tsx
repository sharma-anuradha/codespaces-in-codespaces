import React, { Fragment, useCallback } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { EnvironmentsPanel } from '../environments/environments';
import { CreateAccountPanel } from '../environmentsPanel/create-account-panel';
import { environmentsPath } from '../../routes';

export function NewAccount(props: RouteComponentProps) {

    const hidePanel = useCallback(() => {
        // going back to environments cards (landing page)
        props.history.replace(environmentsPath);
    }, [props.history]);

    return (
        <Fragment>
            <EnvironmentsPanel {...props} />
            <CreateAccountPanel
                    hidePanel={hidePanel}
                />
        </Fragment>
    );
}
