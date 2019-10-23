import React, { Component, ReactComponentElement } from 'react';
import { RouteComponentProps } from 'react-router-dom';
import { connect } from 'react-redux';

import { Spinner } from 'office-ui-fabric-react/lib/Spinner';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Stack } from 'office-ui-fabric-react/lib/Stack';

import { PortalLayout } from '../portalLayout/portalLayout';
import { ILocalCloudEnvironment } from '../../interfaces/cloudenvironment';
import { deleteEnvironment } from '../../actions/deleteEnvironment';
import { ApplicationState } from '../../reducers/rootReducer';
import { shutdownEnvironment } from '../../actions/shutdownEnvironment';
import { PlansReducerState } from '../../reducers/plans-reducer';
import { newPlanPath } from '../../routes';

import { NoEnvironmnets } from './no-environments';
import { NoPlans } from './no-plans';
import { getEnvironmentCardsForCurrentPlan } from './get-environment-cards-for-plan';

import './environments.css';

interface EnvironmentsPanelProps extends RouteComponentProps {
    deleteEnvironment: (...name: Parameters<typeof deleteEnvironment>) => void;
    shutdownEnvironment: (...name: Parameters<typeof shutdownEnvironment>) => void;
    environments: ILocalCloudEnvironment[];
    isLoading: boolean;
    plansStoreState: PlansReducerState;
};

class EnvironmentsPanelView extends Component<EnvironmentsPanelProps & RouteComponentProps> {
    private renderEnvironments() {
        const {
            isLoading,
            environments,
            plansStoreState
        } = this.props;

        if (isLoading) {
            return (
                <Spinner
                    className='environments-panel__environments-spinner'
                    label='Fetching your environments...'
                    ariaLive='assertive'
                    labelPosition='right'
                />
            );
        }

        const { plans, selectedPlan } = plansStoreState;
        if (!plans.length) {
            return (<NoPlans onClick={this.showPlansPanel} />);
        }

        const cards = getEnvironmentCardsForCurrentPlan(selectedPlan, environments);

        return (
            <div className='ms-Grid-row'>
                <Stack horizontal wrap>
                    {
                        (cards.length)
                            ? cards
                            : <NoEnvironmnets onClick={this.showEnvironmentsPanel} />
                    }
                </Stack>
            </div>
        );
    }

    private showPlansPanel = () => {
        this.props.history.push(newPlanPath);
    }

    private showEnvironmentsPanel = () => {
        this.props.history.push('/environments/new');
    }

    render() {
        return (
            <PortalLayout>
                <div className='environments-panel'>
                    <div className='ms-Grid' dir='ltr'>
                        <div className='ms-Grid-row'>
                            <div className='ms-Grid-col ms-sm6 ms-md4 ms-lg9' />
                            <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg3 environments-panel__tar'>
                                <PrimaryButton
                                    text='Create environment'
                                    className='environments-panel__create-button'
                                    onClick={this.showEnvironmentsPanel}
                                    disabled={!this.planValueSelected()}
                                />
                            </div>
                        </div>
                    </div>
                    {this.renderEnvironments()}
                </div>
            </PortalLayout>
        );
    }

    private planValueSelected() {
        const { plansStoreState } = this.props;
        const { selectedPlan } = plansStoreState;

        return !!selectedPlan;
    }
}

const stateToProps = ({ environments: { environments, isLoading }, plans }: ApplicationState) => ({
    environments,
    isLoading,
    plansStoreState: plans
});

const mapDispatch = {
    deleteEnvironment,
    shutdownEnvironment,
};

export const EnvironmentsPanel = connect(
    stateToProps,
    mapDispatch
)(EnvironmentsPanelView);
