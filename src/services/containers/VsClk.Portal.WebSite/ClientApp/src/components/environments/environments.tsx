import React, { Component } from 'react';
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
import { ActivePlanInfo } from '../../reducers/plans-reducer';
import { newPlanPath, newEnvironmentPath } from '../../routerPaths';

import { EnvironmentList } from './get-environment-cards-for-plan';

import './environments.css';
import { isDefined } from '../../utils/isDefined';
import { fetchEnvironments } from '../../actions/fetchEnvironments';
import { pollActivatingEnvironments } from '../../actions/pollEnvironment';
import { environmentIsALie } from '../../utils/environmentUtils';

interface EnvironmentsPanelProps extends RouteComponentProps {
    deleteEnvironment: (...name: Parameters<typeof deleteEnvironment>) => void;
    shutdownEnvironment: (...name: Parameters<typeof shutdownEnvironment>) => void;
    pollActivatingEnvironments(): void;
    pollEnvironmentsForState(): void;
    environments: ILocalCloudEnvironment[];
    isLoading: boolean;
    shouldOpenPlanCreation: boolean;
}

class EnvironmentsPanelView extends Component<EnvironmentsPanelProps> {
    private intervals: (ReturnType<typeof setInterval>)[] = [];
    componentDidMount() {
        this.intervals.push(
            setInterval(() => {
                this.props.pollActivatingEnvironments();
            }, 2000)
        );

        this.intervals.push(
            setInterval(() => {
                this.props.pollEnvironmentsForState();
            }, 1000 * 60 * 2)
        );
    }

    componentWillUnmount() {
        this.intervals.forEach((i) => clearInterval(i));
    }

    private renderEnvironments() {
        const { environments } = this.props;

        const cards = (
            <EnvironmentList
                environments={environments}
                openCreateEnvironmentForm={this.showCreateEnvPanel}
                deleteEnvironment={this.props.deleteEnvironment}
                shutdownEnvironment={this.props.shutdownEnvironment}
            />
        );

        const createEnvironmentButton =
            environments.length === 0 ? null : (
                <div className='ms-Grid' dir='ltr'>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm6 ms-md4 ms-lg9' />
                        <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg3 environments-panel__tar'>
                            <PrimaryButton
                                text='Create environment'
                                className='environments-panel__create-button'
                                onClick={this.showCreateEnvPanel}
                            />
                        </div>
                    </div>
                </div>
            );

        return (
            <div className='ms-Grid-row'>
                {createEnvironmentButton}

                <Stack horizontal wrap>
                    {cards}
                </Stack>
            </div>
        );
    }

    private showCreateEnvPanel = () => {
        if (this.props.shouldOpenPlanCreation) {
            this.props.history.push({
                pathname: newPlanPath,
                search: 'type=wizard',
            });
        } else {
            this.props.history.push(newEnvironmentPath);
        }
    };

    render() {
        const { isLoading } = this.props;

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

        return (
            <PortalLayout>
                <div className='environments-panel'>{this.renderEnvironments()}</div>
            </PortalLayout>
        );
    }
}

function getPlanEnvironments(
    plan: ActivePlanInfo | null,
    environments: ILocalCloudEnvironment[]
): ILocalCloudEnvironment[] {
    if (!plan) {
        return [];
    }

    return (
        environments
            .filter((env) => {
                return !env.planId || env.planId === plan.id;
            })
            // TODO: reintroduce lies once we get better error messaging for environment creation.
            .filter((e) => !environmentIsALie(e))
    );
}

const stateToProps = ({
    environments: { environments, isLoading },
    plans: { selectedPlan },
}: ApplicationState) => ({
    environments: getPlanEnvironments(selectedPlan, environments),
    isLoading,
    shouldOpenPlanCreation: !isDefined(selectedPlan),
});

const mapDispatch = {
    deleteEnvironment,
    shutdownEnvironment,
    pollEnvironmentsForState: fetchEnvironments,
    pollActivatingEnvironments,
};

export const EnvironmentsPanel = connect(
    stateToProps,
    mapDispatch
)(EnvironmentsPanelView);
