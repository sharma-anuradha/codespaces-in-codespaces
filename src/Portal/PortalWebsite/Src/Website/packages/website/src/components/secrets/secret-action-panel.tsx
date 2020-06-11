import React, { useState, useEffect } from 'react';

import { useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import {
    Panel,
    IPanelProps,
    PrimaryButton,
    DefaultButton,
    TextField,
    PanelType,
    MessageBar,
    MessageBarType,
} from 'office-ui-fabric-react';

import {
    ISecret,
    SecretAction,
    ICreateSecretRequest,
    SecretScope,
    ISecretFilter,
    SecretType,
    IUpdateSecretRequest,
    SecretErrorCodes,
} from 'vso-client-core';
import { updateSecret } from '../../actions/updateSecret';
import { createSecret } from '../../actions/createSecret';
import { ApplicationState } from '../../reducers/rootReducer';
import { injectMessageParameters } from '../../utils/injectMessageParameters';
import {
    useValidatableState,
    useTextInput,
    secretErrorCodeToString,
} from '../../utils/secretsUtils';
import { Loader } from '../loader/loader';
import { FiltersList } from './secret-filters';

import './secrets.css';

interface ISecretActionProps extends IPanelProps {
    secret: Partial<ISecret>;
    action: SecretAction;
}

// tslint:disable-next-line: max-func-body-length
export function SecretActionPanel(props: ISecretActionProps) {
    const { t: translation } = useTranslation();
    const { selectedPlan, isUpdatingSecret, isCreatingSecret } = useSelector(
        (state: ApplicationState) => ({
            selectedPlan: state.plans.selectedPlan,
            isUpdatingSecret: state.secrets.isUpdatingSecret,
            isCreatingSecret: state.secrets.isCreatingSecret,
        })
    );
    const [isEditingValue, setIsEditingValue] = useState(false);
    const [isFormValid, setIsFormValid] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | undefined>();

    const {
        value: secretName,
        bind: bindSecretName,
        isModified: isSecretNameModified,
        isValid: isSecretNameValid,
        setIsValid: setIsSecretNameValid,
        resetInput: resetSecretName,
    } = useTextInput(props.secret.secretName);
    const {
        value: secretValue,
        bind: bindsecretValue,
        isModified: isSecretValueModified,
        isValid: isSecretValueValid,
        setIsValid: setIsSecretValueValid,
        resetInput: resetSecretValue,
    } = useTextInput();
    const {
        value: secretNotes,
        bind: bindSecretNotes,
        isModified: isSecretNotesModified,
        isValid: isSecretNotesValid,
        setIsValid: setIsSecretNotesValid,
        resetInput: resetSecretNotes,
    } = useTextInput(props.secret.notes);
    const {
        value: filters,
        isModified: isFiltersModified,
        isValid: isFiltersValid,
        setIsValid: setIsFiltersValid,
        resetState: resetFilters,
        setState: setFilters,
    } = useValidatableState<ISecretFilter[]>(props.secret?.filters || []);

    useEffect(() => {
        const isFormValid = Boolean(
            isSecretNameValid && isSecretValueValid && isSecretNotesValid && isFiltersValid
        );
        setIsFormValid(isFormValid);
    });

    const dismissPanel = () => {
        resetSecretName();
        resetSecretValue();
        resetSecretNotes();
        resetFilters();
        setErrorMessage(undefined);
        props.onDismiss && props.onDismiss();
    };

    const performAction = async () => {
        if (props.action == 'Create') {
            const createRequest = {
                scope: SecretScope.User,
                secretName,
                value: secretValue,
                type: SecretType.EnvironmentVariable,
                filters,
                notes: secretNotes,
            } as ICreateSecretRequest;

            try {
                await createSecret(selectedPlan?.id!, createRequest);
                dismissPanel();
            } catch (err) {
                const errorMessage = secretErrorCodeToString(
                    err.message as SecretErrorCodes,
                    translation
                );
                setErrorMessage(errorMessage);
            }
        } else if (props.action == 'Update') {
            const updateRequest: IUpdateSecretRequest = {
                scope: props.secret.scope!,
            };
            isSecretNameModified && secretName && (updateRequest.secretName = secretName);
            isSecretValueModified && secretValue && (updateRequest.value = secretValue);
            isSecretNotesModified && (updateRequest.notes = secretNotes);
            isFiltersModified && (updateRequest.filters = filters);
            try {
                await updateSecret(selectedPlan?.id!, updateRequest, props.secret.id!);
                dismissPanel();
            } catch (err) {
                const errorMessage = secretErrorCodeToString(
                    err.message as SecretErrorCodes,
                    translation
                );
                setErrorMessage(errorMessage);
            }
        }
    };

    const onRenderFooterContent = () => (
        <div>
            <PrimaryButton
                onClick={performAction}
                className='secret-action-panel__default_button'
                disabled={!isFormValid}
            >
                {props.action == 'Create' ? translation('add') : translation('done')}
            </PrimaryButton>
            <DefaultButton onClick={dismissPanel}>{translation('cancel')}</DefaultButton>
        </div>
    );

    const validateSecretName = (value: string) => {
        if (!value.length) {
            setIsSecretNameValid(false);
            return translation('secretEnvVarNameIsRequired');
        }

        if (value.length > 200) {
            setIsSecretNameValid(false);
            return injectMessageParameters(translation('valueIsTooLong'), '200');
        }

        const invalidPrefixes = ['CODESPACE', 'CLOUDENV'];
        for (const prefix of invalidPrefixes) {
            if (value.startsWith(prefix)) {
                setIsSecretNameValid(false);
                return injectMessageParameters(
                    translation('secretEnvVarNamePrefixIsNotValid'),
                    prefix
                );
            }
        }

        // SecretName can include alphanumeric characters and underscores, and cannot start with a number
        const regex = /^[a-zA-Z_][a-zA-Z0-9_]{0,199}$/g;
        if (!regex.test(value)) {
            setIsSecretNameValid(false);
            return translation('secretEnvVarNameIsNotValid');
        }

        setIsSecretNameValid(true);
    };

    const validateSecretValue = (value: string) => {
        if (!value.length && props.action == 'Create') {
            setIsSecretValueValid(false);
            return translation('secretEnvVarValueIsRequired');
        }
        if (value.length > 1024) {
            setIsSecretValueValid(false);
            return injectMessageParameters(translation('valueIsTooLong'), '1024');
        }
        setIsSecretValueValid(true);
    };

    const validateSecretNotes = (value: string) => {
        if (value.length > 200) {
            setIsSecretNotesValid(false);
            return injectMessageParameters(translation('valueIsTooLong'), '200');
        }
        setIsSecretNotesValid(true);
    };

    return (
        <Panel
            isOpen={props.isOpen}
            onDismiss={dismissPanel}
            headerText={props.headerText}
            closeButtonAriaLabel='Close'
            onRenderFooterContent={onRenderFooterContent}
            isFooterAtBottom={true}
            type={PanelType.medium}
        >
            {isUpdatingSecret && (
                <div className='secret-overlay position-absolute'>
                    <Loader
                        message={translation('secretEnvVarUpdating')}
                        translation={translation}
                    />
                </div>
            )}

            {isCreatingSecret && (
                <div className='secret-overlay position-absolute'>
                    <Loader
                        message={translation('secretEnvVarCreating')}
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
            <TextField
                label={translation('secretEnvVarLabelForName')}
                required
                placeholder={translation('secretEnvVarPlaceholderForName')}
                {...bindSecretName}
                onGetErrorMessage={validateSecretName}
                validateOnLoad={props.action == 'Update'}
            />
            <TextField
                label={translation('secretEnvVarLabelForValue')}
                required={props.action == 'Create'}
                {...bindsecretValue}
                onFocus={(_event) => setIsEditingValue(true)}
                onBlur={(_event) => setIsEditingValue(false)}
                placeholder={
                    props.action == 'Create'
                        ? ''
                        : isEditingValue && !secretValue
                        ? translation('secretEnvVarPlaceholderForValue')
                        : '•••••••••••••••••'
                }
                onGetErrorMessage={validateSecretValue}
                validateOnLoad={props.action == 'Update'}
            />
            <TextField
                label={translation('secretLabelForNotes')}
                {...bindSecretNotes}
                onGetErrorMessage={validateSecretNotes}
            />
            <FiltersList
                filters={filters}
                saveFilters={setFilters}
                isFiltersValid={isFiltersValid}
                setIsFiltersValid={setIsFiltersValid}
                validateOnLoad={true}
            />
        </Panel>
    );
}
