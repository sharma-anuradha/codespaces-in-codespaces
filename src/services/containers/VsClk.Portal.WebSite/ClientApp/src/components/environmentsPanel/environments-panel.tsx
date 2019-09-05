import React, { Component } from 'react';
import { connect } from 'react-redux';
import { Spinner } from 'office-ui-fabric-react/lib/Spinner';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Stack } from 'office-ui-fabric-react/lib/Stack';

import { ILocalCloudEnvironment } from '../../interfaces/cloudenvironment';

import { EnvironmentCard } from '../environmentCard/environment-card';
import { CreateEnvironmentPanel } from './create-environment-panel';

import { createEnvironment } from '../../actions/createEnvironment';
import { deleteEnvironment } from '../../actions/deleteEnvironment';

import { ApplicationState } from '../../reducers/rootReducer';

import { clamp } from '../../utils/clamp';

import './environments-panel.css';

type EnvironmentsPanelProps = {
    createEnvironment: (...name: Parameters<typeof createEnvironment>) => void;
    deleteEnvironment: (...name: Parameters<typeof deleteEnvironment>) => void;
    environments: ILocalCloudEnvironment[];
    isLoading: boolean;
};
export interface EnvironmentsPanelState {
    showPanel: boolean;
}

class EnvironmentsPanelView extends Component<EnvironmentsPanelProps, EnvironmentsPanelState> {
    constructor(props: EnvironmentsPanelProps) {
        super(props);

        this.state = {
            showPanel: false,
        };
    }

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
                    {this.renderEnvironments()}
                </div>
                <CreateEnvironmentPanel
                    onCreateEnvironment={this.onCreateEnvironment}
                    showPanel={this.state.showPanel}
                    hidePanel={this.hidePanel}
                />
            </div>
        );
    }

    private showPanel = () => {
        this.setState({ showPanel: true });
    };

    private hidePanel = () => {
        this.setState({ showPanel: false });
    };

    private onCreateEnvironment = (friendlyName: string, gitRepositoryUrl?: string) => {
        this.props.createEnvironment({
            friendlyName,
            gitRepositoryUrl,
        });
        this.hidePanel();
    };
}

const stateToProps = ({ environments: { environments, isLoading } }: ApplicationState) => ({
    environments,
    isLoading,
});

const mapDispatch = {
    createEnvironment,
    deleteEnvironment,
};

export const EnvironmentsPanel = connect(
    stateToProps,
    mapDispatch
)(EnvironmentsPanelView);
