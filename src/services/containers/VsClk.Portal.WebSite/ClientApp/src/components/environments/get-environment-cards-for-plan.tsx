import React, { ReactComponentElement } from 'react';

import { EnvironmentCard } from '../environmentCard/environment-card';

import { IPlan } from '../../interfaces/IPlan';
import { ILocalCloudEnvironment } from '../../interfaces/cloudenvironment';

import { deleteEnvironment } from '../../actions/deleteEnvironment';
import { shutdownEnvironment } from '../../actions/shutdownEnvironment';


export const getEnvironmentCardsForCurrentPlan = (
    selectedPlan: IPlan | null,
    environments: ILocalCloudEnvironment[]
): ReactComponentElement<typeof EnvironmentCard>[] => {
    if (!selectedPlan) {
        return [];
    }

    const selectedPlanEnvironments = environments.filter((env) => {
        return (!env.planId || (env.planId === selectedPlan.id));
    });

    const cards: ReactComponentElement<typeof EnvironmentCard>[] = selectedPlanEnvironments
        .map((env, i) => {
            const key = env.id || env.lieId || i++;

            return (
                <EnvironmentCard
                    environment={env}
                    deleteEnvironment={deleteEnvironment}
                    shutdownEnvironment={shutdownEnvironment}
                    key={key}
                />
            );
        });

    return cards;
}
