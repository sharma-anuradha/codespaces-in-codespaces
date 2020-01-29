import React, { Component } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import { Panel, IPanel } from 'office-ui-fabric-react/lib/Panel';
import { Toggle } from 'office-ui-fabric-react/lib/Toggle';
import { DefaultButton, PrimaryButton } from 'office-ui-fabric-react/lib/Button';

import { telemetry } from '../../utils/telemetry';
import { ActivePlanInfo } from '../../reducers/plans-reducer';
import { getPlans, selectPlan } from '../../actions/plans-actions';
import { deletePlan } from '../../actions/createPlan';
import { ILocalCloudEnvironment } from '../../interfaces/cloudenvironment';

import './settings-menu-panel.css';
import { PlanSelector } from '../planSelector/plan-selector';
import { Dialog, DialogFooter, DialogType, IDialog, MessageBar, MessageBarType, Dropdown } from 'office-ui-fabric-react';
import { Loader } from '../loader/loader';

interface ISettingsMenuProps extends RouteComponentProps{
    selectedPlan: ActivePlanInfo | null;
    environmentsInPlan: ILocalCloudEnvironment[];
    hidePanel: () => void;
}

interface ISettingsMenuState {
    hideWarning: boolean;
    isDeletingPlan: boolean;
    deleteButtonDisabled: boolean;
    showSuccessMessage: boolean;
    failureMessage: string | undefined;
}

export class SettingsMenuPanel extends Component<ISettingsMenuProps, ISettingsMenuState> {
    public constructor(props: ISettingsMenuProps) {
        super(props);
        let noDelete = true;
        if(this.props.selectedPlan){
            noDelete = false;
        }

        this.state = {
            hideWarning: true,
            isDeletingPlan: false,
            deleteButtonDisabled: noDelete,
            showSuccessMessage: false,
            failureMessage: undefined
        }
    }

    render() {
        let deleteText = <div></div>;
        let envs = <span><b>{this.props.environmentsInPlan.length}</b> environments</span>

        if(this.props.environmentsInPlan.length === 1){
            envs=<span><b>1</b> environment</span>
        }

        if(this.props.selectedPlan){
            deleteText = <div>
                Deleting <b>{this.props.selectedPlan.name}</b> will also delete the {envs} associated with the plan.
                <p/>Do you want to proceed?
            </div>;
        }
        
        return (
            <Panel
                isOpen={true}
                headerText='Settings'
                onDismiss={this.props.hidePanel}
                closeButtonAriaLabel='Close'
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
                        telemetry.setVscodeConfig();
                    }}
                ></Toggle>
                <div className= 'vsonline-settings-panel__separator'/>
                <h3>Plans</h3>
                <div className='vsonline-settings-panel__delete-text'>When a plan is deleted, the associated environments will be deleted as well.</div>
                {this.getPlanSelector()}
                <DefaultButton
                    className='vsonline-settings-panel__delete-button'
                    onClick={() => this.setState({
                        hideWarning: false
                    })}
                    allowDisabledFocus
                    disabled={this.state.deleteButtonDisabled}
                    text="Delete"
                />
                <Dialog
                    hidden={this.state.hideWarning}
                    onDismiss={() => this.setState({
                        hideWarning: true
                    })}
                    dialogContentProps={{
                        type: DialogType.normal,
                        title: 'Warning',
                    }}
                    modalProps={{
                        containerClassName: 'ms-dialogMainOverride'
                    }}
                >
                    {deleteText}
                    <DialogFooter>
                        <PrimaryButton 
                            onClick={() => this.deletePlan(this.props.selectedPlan)}
                            text="Confirm" 
                        />
                        <DefaultButton 
                            onClick={() => this.setState({
                                hideWarning: true
                            })} 
                            text="Cancel" 
                        />
                    </DialogFooter>
                </Dialog>
                <div className= 'vsonline-settings-panel__separator'/>
            </Panel>
        );
    }

    private getPlanSelector() {
        if(this.state.deleteButtonDisabled){
            return(
                <Dropdown
                    className='vsonline-settings-panel__plan-selector'
                    defaultSelectedKey="noPlans"
                    options={[{key: 'noPlans', text: 'No plans available'}]}
                    disabled={true}
                />
            )
        }
        return(
            <PlanSelector className='vsonline-settings-panel__plan-selector' hasNoCreate={true} {...this.props}/>
        )     
    }

    private renderOverlay() {
        const { isDeletingPlan } = this.state;

        if (!isDeletingPlan ){
            return null;
        }

        return(
            <div className='settings-panel__overlay'>
                <Loader message='Deleting the plan...' />
            </div>
        )
    }

    private renderSuccessMessage() {
        const { showSuccessMessage } = this.state;
        if(!showSuccessMessage){
            return null;
        }

        return(
            <MessageBar
                messageBarType={MessageBarType.success}
                isMultiline={false}
                onDismiss={() => this.setState({ showSuccessMessage: false })}
            >
                Your plan was successfully deleted.
            </MessageBar>
        )
    }

    private renderFailureMessage() {
        const { failureMessage } = this.state
        if(!failureMessage){
            return null;
        }

        return(
            <MessageBar
                messageBarType={MessageBarType.error}
                isMultiline={false}
                onDismiss={() => this.setState({ failureMessage: undefined })}
            >
                {this.state.failureMessage}
            </MessageBar>
        )
    }

    private async deletePlan(selectedPlan: ActivePlanInfo | null){ 
        this.setState({
            hideWarning: true
        });

        if(selectedPlan){
            this.setState({ isDeletingPlan: true });

            let errorMessage = await deletePlan(selectedPlan.id);

            if(errorMessage){
                this.setState({
                    failureMessage: errorMessage,
                    isDeletingPlan: false
                })
                return;
            }

            let newPlansList = await getPlans();

            if(newPlansList.length > 0){
                selectPlan(newPlansList[0]);
            }
            else{
                this.setState({ deleteButtonDisabled: true });
            }
            
            this.setState({ 
                showSuccessMessage: true,
                isDeletingPlan: false 
            });

        }
        else{
            this.setState({
                failureMessage: "No plan selected"
            })
        }
        
    }
}

