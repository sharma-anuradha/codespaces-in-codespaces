import React, { useState, useCallback } from 'react';
import { RouteComponentProps } from 'react-router-dom';
import { useSelector } from 'react-redux';
import {
    Toggle,
    DefaultButton,
    Dialog,
    DialogType,
    DialogFooter,
    PrimaryButton,
    MessageBar,
    MessageBarType,
    Dropdown,
} from 'office-ui-fabric-react';

import { ILocalEnvironment, isHostedOnGithub } from 'vso-client-core';
import { getVSCodeVersion } from 'vso-workbench';
import { telemetry } from 'vso-workbench/src/telemetry/telemetry';

import { deletePlan } from '../../actions/createPlan';
import { getPlans, selectPlan } from '../../actions/plans-actions';

import { ApplicationState } from '../../reducers/rootReducer';
import { ActivePlanInfo } from '../../reducers/plans-reducer';

import { getPlanEnvironments } from '../environments/environments';
import { PortalLayout } from '../portalLayout/portalLayout';
import { PlanSelector } from '../planSelector/plan-selector';
import { Loader } from '../loader/loader';

import './settings-menu.css';

const setTelemetryVSCodeConfig = () => {
    const vscodeConfig = getVSCodeVersion();
    telemetry.setVscodeConfig(vscodeConfig.commit, vscodeConfig.quality);
};

interface IPlanSelectorWrapperProps extends RouteComponentProps {
    canDeletePlan: boolean;
}

interface IDeletePlanWarningMessageProps {
    selectedPlan: ActivePlanInfo | null;
    environments: ILocalEnvironment[];
}

function DeletePlanWarningMessage(props: IDeletePlanWarningMessageProps) {
    return (
        props.selectedPlan && (
            <div>
                Deleting <b>{props.selectedPlan.name}</b> will also delete the
                {props.environments.length == 1 ? (
                    <span>
                        <b> 1</b> Codespace
                    </span>
                ) : (
                    <span>
                        <b> {props.environments.length}</b> Codespaces
                    </span>
                )}{' '}
                associated with the plan.
                <p />
                Do you want to proceed?
            </div>
        )
    );
}

function PlanSelectorWrapper(props: IPlanSelectorWrapperProps) {
    return props.canDeletePlan ? (
        <PlanSelector
            className='vsonline-settings-menu__plan-selector'
            hasNoCreate={true}
            {...props}
        />
    ) : (
        <Dropdown
            className='vsonline-settings-menu__plan-selector'
            defaultSelectedKey='noPlans'
            options={[{ key: 'noPlans', text: 'No plans available' }]}
            disabled={true}
            ariaLabel='Plan Dropdown'
        />
    );
}

// tslint:disable-next-line: max-func-body-length
export function SettingsMenu(props: RouteComponentProps) {
    const { selectedPlan, environments } = useSelector((state: ApplicationState) => ({
        selectedPlan: state.plans.selectedPlan,
        environments: state.environments.environments,
    }));

    const [showWarning, setShowWarning] = useState<boolean>(false);
    const [isDeletingPlan, setIsDeletingPlan] = useState<boolean>(false);
    const [canDeletePlan, setCanDeletePlan] = useState<boolean>(!!selectedPlan);
    const [successMessage, setSuccessMessage] = useState<string | undefined>(undefined);
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    const deleteSelectedPlan = useCallback(
        async (selectedPlan: ActivePlanInfo | null) => {
            setShowWarning(false);

            if (selectedPlan) {
                setIsDeletingPlan(true);

                const errorMessage = await deletePlan(selectedPlan.id);

                if (errorMessage) {
                    setErrorMessage(errorMessage);
                    setIsDeletingPlan(false);
                    return;
                }

                let newPlansList = await getPlans();

                if (newPlansList.length > 0) {
                    selectPlan(newPlansList[0]);
                } else {
                    setCanDeletePlan(false);
                }

                setSuccessMessage('Your plan was successfully deleted.');
                setIsDeletingPlan(false);
            } else {
                setErrorMessage('No plan selected');
            }
        },
        [selectedPlan]
    );

    return (
        <PortalLayout hideNavigation={isHostedOnGithub()}>
            <div className='settings-menu ms-Grid-row ms-Fabric'>
                <h2>Settings</h2>

                {isDeletingPlan && (
                    <div className='settings-menu__overlay'>
                        <Loader message='Deleting the plan...' />
                    </div>
                )}

                {successMessage && (
                    <MessageBar
                        messageBarType={MessageBarType.success}
                        isMultiline={false}
                        onDismiss={() => setSuccessMessage(undefined)}
                        dismissButtonAriaLabel='Dismiss plan deletion success message.'
                    >
                        {successMessage}
                    </MessageBar>
                )}

                {errorMessage && (
                    <MessageBar
                        messageBarType={MessageBarType.error}
                        isMultiline={false}
                        onDismiss={() => setErrorMessage(undefined)}
                        dismissButtonAriaLabel='Dismiss plan deletion error message.'
                    >
                        {errorMessage}
                    </MessageBar>
                )}

                <div className='vsonline-settings-menu__section' />
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
                <div className='vsonline-settings-menu__section vsonline-settings-menu__separator' />
                <h3>Plans</h3>
                <div className='vsonline-settings-menu__delete-text'>
                    When a plan is deleted, the associated Codespaces will be deleted as well.
                </div>
                <PlanSelectorWrapper {...props} canDeletePlan={canDeletePlan} />
                <DefaultButton
                    className='vsonline-settings-menu__delete-button'
                    onClick={() => setShowWarning(true)}
                    allowDisabledFocus
                    disabled={!canDeletePlan}
                    text='Delete'
                />
                <div className='vsonline-settings-menu__section vsonline-settings-menu__separator' />
                <div id='target'></div>
            </div>
            <Dialog
                styles={{
                    root: {
                        position: 'absolute',
                    },
                }}
                hidden={!showWarning}
                onDismiss={() => setShowWarning(false)}
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
                <DeletePlanWarningMessage
                    selectedPlan={selectedPlan}
                    environments={getPlanEnvironments(selectedPlan, environments)}
                />
                <DialogFooter>
                    <PrimaryButton
                        onClick={() => deleteSelectedPlan(selectedPlan)}
                        text='Confirm'
                    />
                    <DefaultButton onClick={() => setShowWarning(false)} text='Cancel' />
                </DialogFooter>
            </Dialog>
        </PortalLayout>
    );
}
