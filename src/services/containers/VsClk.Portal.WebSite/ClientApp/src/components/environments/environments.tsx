import React, { Component } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { connect } from 'react-redux';
import { Spinner } from 'office-ui-fabric-react/lib/Spinner';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { PortalLayout } from '../portalLayout/portalLayout';
import { ILocalCloudEnvironment } from '../../interfaces/cloudenvironment';
import { EnvironmentCard } from '../environmentCard/environment-card';
import { deleteEnvironment } from '../../actions/deleteEnvironment';
import { ApplicationState } from '../../reducers/rootReducer';
import { clamp } from '../../utils/clamp';
import './environments.css';

type EnvironmentsPanelProps = {
    deleteEnvironment: (...name: Parameters<typeof deleteEnvironment>) => void;
    environments: ILocalCloudEnvironment[];
    isLoading: boolean;
};

class EnvironmentsPanelView extends Component<EnvironmentsPanelProps & RouteComponentProps> {
    private renderEnvironments() {
        const { isLoading, environments, deleteEnvironment } = this.props;

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

        const cards = [];
        let i = 0;
        for (const env of clamp(environments, 5)) {
            const key = env.id || env.lieId || i++;
            cards.push(
                <EnvironmentCard
                    environment={env}
                    deleteEnvironment={deleteEnvironment}
                    key={key}
                />
            );
        }

        return (
            <div className='ms-Grid-row'>
                <Stack horizontal wrap>
                    {cards.length ? cards : this.renderNoEnvironments()}
                </Stack>
            </div>
        );
    }

    private renderNoEnvironments() {
        return (
            <div className='environments-panel__no-environments' key='no-envs'>
                No environments so far. <Link onClick={this.showPanel}>Create one!</Link>
            </div>
        );
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
                                    onClick={this.showPanel}
                                />
                            </div>
                        </div>
                    </div>
                    {this.renderEnvironments()}
                </div>
            </PortalLayout>
        );
    }

    private showPanel = () => {
        this.props.history.replace('/environments/new');
    };
}

const stateToProps = ({ environments: { environments, isLoading } }: ApplicationState) => ({
    environments,
    isLoading,
});

const mapDispatch = {
    deleteEnvironment,
};

export const EnvironmentsPanel = connect(
    stateToProps,
    mapDispatch
)(EnvironmentsPanelView);
