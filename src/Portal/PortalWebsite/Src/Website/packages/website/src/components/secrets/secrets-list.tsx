import React, { Fragment, useState, useCallback } from 'react';

import { useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { TFunction } from 'i18next';
import { useConstCallback } from '@uifabric/react-hooks';
import {
    DetailsList,
    DetailsListLayoutMode,
    IColumn,
    SelectionMode,
    ActionButton,
    IContextualMenuProps,
    IconButton,
    IIconProps,
    List,
    Dialog,
    DefaultButton,
    PrimaryButton,
    DialogFooter,
    DialogType,
    MessageBar,
    MessageBarType,
} from 'office-ui-fabric-react';

import {
    ISecret,
    SecretType,
    SecretScope,
    ISecretFilter,
    SecretAction,
    SecretErrorCodes,
} from 'vso-client-core';
import { deleteSecret } from '../../actions/deleteSecret';
import { ActivePlanInfo } from '../../reducers/plans-reducer';
import { ApplicationState } from '../../reducers/rootReducer';
import { injectMessageParameters } from '../../utils/injectMessageParameters';
import { secretErrorCodeToString } from '../../utils/secretsUtils';

import { Loader } from '../loader/loader';
import { SecretActionPanel } from './secret-action-panel';
import { ReadonlyFiltersList } from './secret-filters';

import './secrets.css';

export function SecretsList() {
    const { t: translation } = useTranslation();
    const { secrets, isLoadingSecrets, isDeletingSecret, selectedPlan } = useSelector(
        (state: ApplicationState) => ({
            secrets: state.secrets.secrets,
            isLoadingSecrets: state.secrets.isLoadingSecrets,
            isDeletingSecret: state.secrets.isDeletingSecret,
            selectedPlan: state.plans.selectedPlan,
        })
    );

    const [isSecretCreatePanelOpen, setIsSecretCreatePanelOpen] = useState<boolean>(false);
    const [errorMessage, setErrorMessage] = useState<string>();

    const setError = (errorMessage: string) => setErrorMessage(errorMessage);

    const dismissPanel = useCallback(() => setIsSecretCreatePanelOpen(false), [
        isSecretCreatePanelOpen,
    ]);

    return (
        <Fragment>
            <h3>{translation('secretEnvVarTitle')}</h3>
            <ActionButton
                iconProps={{ iconName: 'Add' } as IIconProps}
                disabled={!selectedPlan}
                onClick={(_event) => {
                    setIsSecretCreatePanelOpen(true);
                }}
                ariaLabel={translation('add')}
            >
                {translation('add')}
            </ActionButton>
            {isLoadingSecrets && (
                <div className='secret-overlay position-relative'>
                    <Loader
                        message={translation('secretEnvVarLoading')}
                        translation={translation}
                    />
                </div>
            )}
            {isDeletingSecret && (
                <div className='secret-overlay position-relative'>
                    <Loader
                        message={translation('secretEnvVarDeleting')}
                        translation={translation}
                    />
                </div>
            )}
            {errorMessage && (
                <MessageBar
                    messageBarType={MessageBarType.error}
                    isMultiline={false}
                    onDismiss={() => setErrorMessage(undefined)}
                    dismissButtonAriaLabel={translation('dismissErrorMessage')}
                >
                    {errorMessage}
                </MessageBar>
            )}
            <DetailsList
                items={secrets}
                columns={getColumns(selectedPlan, setError, translation)}
                layoutMode={DetailsListLayoutMode.justified}
                selectionMode={SelectionMode.none}
                compact={false}
            />

            <SecretActionPanel
                isOpen={isSecretCreatePanelOpen}
                onDismiss={dismissPanel}
                headerText={translation('secretEnvVarCreateHeader')}
                closeButtonAriaLabel={translation('close')}
                secret={{ scope: SecretScope.User, type: SecretType.EnvironmentVariable }}
                action='Create'
            />
        </Fragment>
    );
}

interface ISecretActionMenuButtonProps {
    readonly secret: ISecret;
    readonly selectedPlan: ActivePlanInfo | null;
    readonly setError: (errorMessage: string) => void;
}

function SecretActionMenuButton(props: ISecretActionMenuButtonProps) {
    const { t: translation } = useTranslation();
    const [isSecretEditPanelOpen, setIsSecretEditPanelOpen] = useState(false);
    const [isSecretDeleteWarningOpen, setIsSecretDeleteWarningOpen] = useState(false);
    const { isDeletingSecret } = useSelector((state: ApplicationState) => ({
        isDeletingSecret: state.secrets.isDeletingSecret,
        selectedPlan: state.plans.selectedPlan,
    }));

    const dismissPanel = useCallback(() => setIsSecretEditPanelOpen(false), [
        isSecretEditPanelOpen,
    ]);

    const performSecretAction = useConstCallback((action: SecretAction) => {
        if (action == 'Update') {
            setIsSecretEditPanelOpen(true);
        } else if (action == 'Delete') {
            setIsSecretDeleteWarningOpen(true);
        }
    });

    const performSecretDeletion = useConstCallback(() => {
        setIsSecretDeleteWarningOpen(false);
        try {
            deleteSecret(props.selectedPlan?.id!, props.secret.id, props.secret.scope);
        } catch (err) {
            const errorMessage = secretErrorCodeToString(
                err.message as SecretErrorCodes,
                translation
            );
            props.setError(errorMessage);
        }
    });

    const menuProps: IContextualMenuProps = {
        items: [
            {
                key: 'edit',
                text: translation('edit'),
                iconProps: { iconName: 'Edit' },
                onClick: () => performSecretAction('Update'),
            },
            {
                key: 'delete',
                text: translation('delete'),
                iconProps: { iconName: 'Delete' },
                onClick: () => performSecretAction('Delete'),
            },
        ],
        isBeakVisible: false,
    };

    return (
        <Fragment>
            <IconButton
                menuProps={menuProps}
                iconProps={{}}
                menuIconProps={{ iconName: 'MoreVertical', style: { fontSize: '1.6rem' } }}
                title={translation('action')}
                disabled={isDeletingSecret}
            />
            {isSecretEditPanelOpen && (
                <SecretActionPanel
                    isOpen={isSecretEditPanelOpen}
                    onDismiss={dismissPanel}
                    headerText={translation('secretEnvVarEditHeader')}
                    closeButtonAriaLabel={translation('close')}
                    secret={props.secret}
                    action='Update'
                />
            )}
            <Dialog
                styles={{ root: { position: 'absolute' } }}
                hidden={!isSecretDeleteWarningOpen}
                onDismiss={() => setIsSecretDeleteWarningOpen(false)}
                dialogContentProps={{
                    type: DialogType.normal,
                    title: injectMessageParameters(
                        translation('secretEnvVarDeleteTitle'),
                        props.secret.secretName
                    ),
                    subText: translation('secretEnvVarDeleteMessage'),
                }}
                modalProps={{
                    isBlocking: true,
                    styles: { main: { maxWidth: 650 } },
                    containerClassName: 'ms-dialogMainOverride',
                }}
            >
                <DialogFooter>
                    <PrimaryButton onClick={performSecretDeletion} text={translation('delete')} />
                    <DefaultButton
                        onClick={() => setIsSecretDeleteWarningOpen(false)}
                        text={translation('cancel')}
                    />
                </DialogFooter>
            </Dialog>
        </Fragment>
    );
}

const getColumns = (
    selectedPlan: ActivePlanInfo | null,
    setError: (errorMessage: string) => void,
    translation: TFunction
): IColumn[] => {
    return [
        {
            key: 'secretName',
            name: translation('secretEnvVarLabelForName'),
            fieldName: 'secretName',
            minWidth: 100,
            maxWidth: 200,
            isResizable: true,
        },
        {
            key: 'value',
            name: translation('secretEnvVarLabelForValue'),
            minWidth: 50,
            maxWidth: 100,
            isResizable: true,
            onRender: () => <>•••••••••••••••••</>,
        },
        {
            key: 'filters',
            name: translation('secretLabelForFilters'),
            minWidth: 200,
            maxWidth: 300,
            isResizable: true,
            onRender: (item: ISecret) => (
                <List
                    items={item.filters}
                    onRenderCell={(item: ISecretFilter | undefined) => (
                        <ReadonlyFiltersList item={item} />
                    )}
                />
            ),
        },
        {
            key: 'notes',
            name: translation('secretLabelForNotes'),
            fieldName: 'notes',
            minWidth: 100,
            maxWidth: 200,
            isResizable: true,
        },
        {
            key: 'action',
            name: translation('action'),
            minWidth: 16,
            maxWidth: 16,
            isResizable: false,
            isIconOnly: true,
            onRender: (item: ISecret) => (
                <SecretActionMenuButton
                    secret={item}
                    selectedPlan={selectedPlan}
                    setError={setError}
                />
            ),
        },
    ];
};
