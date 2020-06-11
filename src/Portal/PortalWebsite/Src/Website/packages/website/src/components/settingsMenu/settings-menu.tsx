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

import { deletePlan } from '../../actions/deletePlan';

import { ApplicationState } from '../../reducers/rootReducer';
import { useTranslation } from 'react-i18next';
import { ActivePlanInfo } from '../../reducers/plans-reducer';
import { injectMessageParametersJSX } from '../../utils/injectMessageParameters';
import { getPlanEnvironments } from '../environments/environments';

import { PortalLayout } from '../portalLayout/portalLayout';
import { PlanSelector } from '../planSelector/plan-selector';
import { Loader } from '../loader/loader';
import { SecretsList } from '../secrets/secrets-list';

import './settings-menu.css';

const setTelemetryVSCodeConfig = () => {
    const vscodeConfig = getVSCodeVersion();
    telemetry.setVscodeConfig(vscodeConfig.commit, vscodeConfig.quality);
};

interface IPlanSelectorWrapperProps extends RouteComponentProps {
    selectedPlan: ActivePlanInfo | null;
    isDeletingPlan: boolean;
    isLoadingPlan: boolean;
}

interface IDeletePlanWarningMessageProps {
    selectedPlan: ActivePlanInfo | null;
    environments: ILocalEnvironment[];
}

function DeletePlanWarningMessage(props: IDeletePlanWarningMessageProps) {
    const { t: translation } = useTranslation();
    if (props.selectedPlan) {
        const codespacesElement =
            props.environments.length == 1 ? (
                <span>
                    <b> 1</b> {translation('codespace')}
                </span>
            ) : (
                <span>
                    <b> {props.environments.length}</b> {translation('codespaces')}
                </span>
            );

        const deleteWarning = injectMessageParametersJSX(
            translation('deletePlanWarning'),
            <b>{props.selectedPlan.name}</b>,
            codespacesElement
        );
        return (
            <div>
                {deleteWarning}
                <p />
                {translation('wantToProceed')}
            </div>
        );
    }
    return null;
}

function PlanSelectorWrapper(props: IPlanSelectorWrapperProps) {
    return props.selectedPlan || props.isDeletingPlan || props.isLoadingPlan ? (
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
    const { selectedPlan, isLoadingPlan, environments } = useSelector(
        (state: ApplicationState) => ({
            selectedPlan: state.plans.selectedPlan,
            isLoadingPlan: state.plans.isLoadingPlan,
            environments: state.environments.environments,
        })
    );

    const [showWarning, setShowWarning] = useState<boolean>(false);
    const [isDeletingPlan, setIsDeletingPlan] = useState<boolean>(false);
    const [successMessage, setSuccessMessage] = useState<string | undefined>(undefined);
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);
    const { t: translation } = useTranslation();

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

                setSuccessMessage(translation('planDeleteSucceeded'));
                setIsDeletingPlan(false);
            } else {
                setErrorMessage(translation('noPlanSelected'));
            }
        },
        [selectedPlan]
    );

    return (
        <PortalLayout hideNavigation={isHostedOnGithub()}>
            <div className='settings-menu ms-Grid-row ms-Fabric'>
                <h2>{translation('settings')}</h2>

                {isDeletingPlan && (
                    <div className='settings-menu__overlay'>
                        <Loader message={translation('deletingPlan')} translation={translation} />
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
                <h3>{translation('insidersChannel')}</h3>
                <Toggle
                    defaultChecked={window.localStorage.getItem('vso-featureset') === 'insider'}
                    onText={translation('on')}
                    offText={translation('off')}
                    onChange={(e, checked) => {
                        window.localStorage.setItem(
                            'vso-featureset',
                            checked ? 'insider' : 'stable'
                        );
                        setTelemetryVSCodeConfig();
                    }}
                ></Toggle>
                <div className='vsonline-settings-menu__section vsonline-settings-menu__separator' />
                <h3>{translation('plans')}</h3>
                <div className='vsonline-settings-menu__delete-text'>
                    {translation('deletePlanInfo')}
                </div>
                <PlanSelectorWrapper
                    {...props}
                    selectedPlan={selectedPlan}
                    isDeletingPlan={isDeletingPlan}
                    isLoadingPlan={isLoadingPlan}
                />
                <DefaultButton
                    className='vsonline-settings-menu__delete-button'
                    onClick={() => setShowWarning(true)}
                    allowDisabledFocus
                    disabled={!selectedPlan}
                    text={translation('delete')}
                />
                <div className='vsonline-settings-menu__section vsonline-settings-menu__separator' />
                <SecretsList />
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
                    title: translation('warning'),
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
                        text={translation('confirm')}
                    />
                    <DefaultButton
                        onClick={() => setShowWarning(false)}
                        text={translation('cancel')}
                    />
                </DialogFooter>
            </Dialog>
        </PortalLayout>
    );
}
