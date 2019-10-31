import React, { FC, Fragment } from 'react';

import { NoEnvironments } from './no-environments';
import { EnvironmentCard } from '../environmentCard/environment-card';

import { ILocalCloudEnvironment } from '../../interfaces/cloudenvironment';

import { deleteEnvironment } from '../../actions/deleteEnvironment';
import { shutdownEnvironment } from '../../actions/shutdownEnvironment';

interface IProps {
    environments: ILocalCloudEnvironment[];
    openCreateEnvironmentForm: () => void;
    deleteEnvironment: (...name: Parameters<typeof deleteEnvironment>) => void;
    shutdownEnvironment: (...name: Parameters<typeof shutdownEnvironment>) => void;
}

export const EnvironmentList: FC<IProps> = ({
    environments,
    openCreateEnvironmentForm,
    deleteEnvironment,
    shutdownEnvironment,
}) => {
    if (environments.length === 0) {
        return <NoEnvironments onClick={openCreateEnvironmentForm} />;
    }

    const cards = environments.map((env) => {
        const key = env.id || env.lieId;

        return (
            <EnvironmentCard
                environment={env}
                deleteEnvironment={deleteEnvironment}
                shutdownEnvironment={shutdownEnvironment}
                key={key}
            />
        );
    });

    return <Fragment>{cards}</Fragment>;
};
