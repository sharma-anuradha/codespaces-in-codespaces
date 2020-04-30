import React, { Component, Fragment } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { Panel } from 'office-ui-fabric-react/lib/Panel';
import { Toggle } from 'office-ui-fabric-react/lib/Toggle';
import { DefaultButton, PrimaryButton } from 'office-ui-fabric-react/lib/Button';

import {
    Dialog,
    DialogFooter,
    DialogType,
    MessageBar,
    MessageBarType,
    Dropdown,
} from 'office-ui-fabric-react';

import { ILocalEnvironment } from 'vso-client-core';
import { getVSCodeVersion } from 'vso-workbench';

import { telemetry } from '../../utils/telemetry';
import { ActivePlanInfo } from '../../reducers/plans-reducer';
import { getPlans, selectPlan } from '../../actions/plans-actions';
import { deletePlan } from '../../actions/createPlan';

import { PlanSelector } from '../planSelector/plan-selector';
import { Loader } from '../loader/loader';

import './settings-menu-panel.css';

interface ISettingsMenuProps extends RouteComponentProps {
    selectedPlan: ActivePlanInfo | null;
    environmentsInPlan: ILocalEnvironment[];
    hidePanel: () => void;
}

interface ISettingsMenuState {
    hideWarning: boolean;
    isDeletingPlan: boolean;
    deleteButtonDisabled: boolean;
    showSuccessMessage: boolean;
    failureMessage: string | undefined;
}

const setTelemetryVSCodeConfig = () => {
    const vscodeConfig = getVSCodeVersion();
    telemetry.setVscodeConfig(vscodeConfig.commit, vscodeConfig.quality);
};

export class SettingsMenuPanel extends Component<ISettingsMenuProps, ISettingsMenuState> {
    public constructor(props: ISettingsMenuProps) {
        super(props);
        let noDelete = true;
        if (this.props.selectedPlan) {
            noDelete = false;
        }

        this.state = {
            hideWarning: true,
            isDeletingPlan: false,
            deleteButtonDisabled: noDelete,
            showSuccessMessage: false,
            failureMessage: undefined,
        };
    }

    private hideWarning = () => {
        this.setState({
            hideWarning: true,
        });
    };

    private showWarning = () => {
        this.setState({
            hideWarning: false,
        });
    };

    render() {
        let deleteText = <div></div>;
        let envs = (
            <span>
                <b>{this.props.environmentsInPlan.length}</b> Codespaces
            </span>
        );
        if (this.props.environmentsInPlan.length === 1) {
            envs = (
                <span>
                    <b>1</b> Codespace
                </span>
            );
        }
        if (this.props.selectedPlan) {
            deleteText = (
                <div>
                    Deleting <b>{this.props.selectedPlan.name}</b> will also delete the {envs}{' '}
                    associated with the plan.
                    <p />
                    Do you want to proceed?
                </div>
            );
        }

        return (
            <Fragment>
                <Panel
                    isOpen={true}
                    headerText='Settings'
                    onDismiss={this.props.hidePanel}
                    closeButtonAriaLabel='Close'
                    id='settingsPanel'
                >
                    {this.renderOverlay()}
                    {this.renderSuccessMessage()}
                    {this.renderFailureMessage()}
                    <h3>Insiders channel</h3>
                    <Toggle
                        defaultChecked={window.localStorage.getItem('vso-featureset') === 'insider'}
                        onText='On'
                        offText='Off'
                        onChange={(e, checked) => {
                            window.localStorage.setItem(
                                'vso-featureset',
                                checked ? 'insider' : 'stable'
                            );

                            setTelemetryVSCodeConfig();
                        }}
                    ></Toggle>
                    <div className='vsonline-settings-panel__separator' />
                    <h3>Plans</h3>
                    <div className='vsonline-settings-panel__delete-text'>
                        When a plan is deleted, the associated Codespaces will be deleted as well.
                    </div>
                    {this.getPlanSelector()}
                    <DefaultButton
                        className='vsonline-settings-panel__delete-button'
                        onClick={this.showWarning}
                        allowDisabledFocus
                        disabled={this.state.deleteButtonDisabled}
                        text='Delete'
                    />
                    <div className='vsonline-settings-panel__separator' />
                    <div id='target'></div>
                </Panel>
                <Dialog
                    styles={{
                        root: {
                            position: 'absolute',
                        },
                    }}
                    hidden={this.state.hideWarning}
                    onDismiss={this.hideWarning}
                    dialogContentProps={{
                        type: DialogType.normal,
                        title: 'Warning',
                    }}
                    modalProps={{
                        layerProps: {
                            hostId: 'target',
                        },
                        isBlocking: false,
                        styles: { main: { maxWidth: 450 } },
                        containerClassName: 'ms-dialogMainOverride',
                    }}
                >
                    {deleteText}
                    <DialogFooter>
                        <PrimaryButton
                            onClick={() => this.deletePlan(this.props.selectedPlan)}
                            text='Confirm'
                        />
                        <DefaultButton onClick={this.hideWarning} text='Cancel' />
                    </DialogFooter>
                </Dialog>
            </Fragment>
        );
    }

    private getPlanSelector() {
        if (this.state.deleteButtonDisabled) {
            return (
                <Dropdown
                    className='vsonline-settings-panel__plan-selector'
                    defaultSelectedKey='noPlans'
                    options={[{ key: 'noPlans', text: 'No plans available' }]}
                    disabled={true}
                />
            );
        }
        return (
            <PlanSelector
                className='vsonline-settings-panel__plan-selector'
                hasNoCreate={true}
                {...this.props}
            />
        );
    }

    private renderOverlay() {
        const { isDeletingPlan } = this.state;

        if (!isDeletingPlan) {
            return null;
        }

        return (
            <div className='settings-panel__overlay'>
                <Loader message='Deleting the plan...' />
            </div>
        );
    }

    private hideSuccessMessage = () => {
        this.setState({ showSuccessMessage: false });
    };

    private renderSuccessMessage() {
        const { showSuccessMessage } = this.state;
        if (!showSuccessMessage) {
            return null;
        }

        return (
            <MessageBar
                messageBarType={MessageBarType.success}
                isMultiline={false}
                onDismiss={this.hideSuccessMessage}
            >
                Your plan was successfully deleted.
            </MessageBar>
        );
    }

    private renderFailureMessage() {
        const { failureMessage } = this.state;
        if (!failureMessage) {
            return null;
        }

        return (
            <MessageBar
                messageBarType={MessageBarType.error}
                isMultiline={false}
                onDismiss={() => this.setState({ failureMessage: undefined })}
            >
                {this.state.failureMessage}
            </MessageBar>
        );
    }

    private async deletePlan(selectedPlan: ActivePlanInfo | null) {
        this.hideWarning();

        if (selectedPlan) {
            this.setState({ isDeletingPlan: true });

            let errorMessage = await deletePlan(selectedPlan.id);

            if (errorMessage) {
                this.setState({
                    failureMessage: errorMessage,
                    isDeletingPlan: false,
                });
                return;
            }

            let newPlansList = await getPlans();

            if (newPlansList.length > 0) {
                selectPlan(newPlansList[0]);
            } else {
                this.setState({ deleteButtonDisabled: true });
            }

            this.setState({
                showSuccessMessage: true,
                isDeletingPlan: false,
            });
        } else {
            this.setState({
                failureMessage: 'No plan selected',
            });
        }
    }
}
