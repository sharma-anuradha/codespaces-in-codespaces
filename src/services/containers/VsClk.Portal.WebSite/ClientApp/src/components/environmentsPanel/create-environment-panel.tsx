import React, { Component, KeyboardEvent, SyntheticEvent, ReactElement, Fragment } from 'react';
import { connect } from 'react-redux';

import { PrimaryButton, DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { KeyCodes } from '@uifabric/utilities';
import { Link } from 'office-ui-fabric-react/lib/components/Link';
import { Dropdown, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';

import { useWebClient, ServiceResponseError } from '../../actions/middleware/useWebClient';
import { createEnvironment } from '../../actions/createEnvironment';
import { storeGitHubCredentials } from '../../actions/getGitHubCredentials';
import { ApplicationState } from '../../reducers/rootReducer';
import { GithubAuthenticationAttempt } from '../../services/gitHubAuthenticationService';
import { Collapsible } from '../collapsible/collapsible';

import {
    normalizeGitUrl,
    getSupportedGitService,
    getQueryableUrl,
    SupportedGitService,
} from '../../utils/gitUrlNormalization';

import './create-environment-panel.css';

type CreateEnvironmentParams = Parameters<typeof createEnvironment>[0];

async function queryGitService(url: string, bearerToken?: string): Promise<boolean> {
    const webClient = useWebClient();

    const headers: Record<string, string> = {};
    if (bearerToken) {
        headers['Authorization'] = `Bearer ${bearerToken}`;
    }

    try {
        await webClient.request(
            url.toString(),
            { headers },
            { skipParsingResponse: true, requiresAuthentication: false }
        );
        return true;
    } catch (err) {
        if (err instanceof ServiceResponseError) {
            return false;
        }

        throw err;
    }
}

export async function validateGitRepository(
    maybeRepository: string,
    gitHubAccessToken: string | null = null,
    required = false
): Promise<string> {
    const valid = '';
    const maybeGitUrl = normalizeGitUrl(maybeRepository);

    if (!required && !maybeRepository) {
        return valid;
    }

    if (!maybeGitUrl) {
        return validationMessages.invalidGitUrl;
    }

    const gitServiceProvider = getSupportedGitService(maybeGitUrl);
    const queryableUrl = getQueryableUrl(maybeGitUrl);
    if (!queryableUrl) {
        return validationMessages.invalidGitUrl;
    }

    if (gitServiceProvider === SupportedGitService.GitHub && gitHubAccessToken) {
        const isAccessible = await queryGitService(queryableUrl, gitHubAccessToken);
        if (!isAccessible) {
            return validationMessages.noAccess;
        } else {
            return valid;
        }
    } else if (gitServiceProvider === SupportedGitService.GitHub) {
        try {
            const isAccessible = await queryGitService(queryableUrl);
            if (!isAccessible) {
                return validationMessages.privateRepoNoAuth;
            } else {
                return valid;
            }
        } catch {
            return validationMessages.testFailed;
        }
    } else {
        try {
            if (await queryGitService(queryableUrl)) {
                return valid;
            } else {
                return validationMessages.unableToConnect;
            }
        } catch {
            return validationMessages.testFailed;
        }
    }
}

function getValidationIcon(state: TextFieldState) {
    if (!state.isRequired && !state.value) {
        return undefined;
    }

    switch (state.validation) {
        case ValidationState.Valid:
            return { iconName: 'CheckMark' };

        default:
            return undefined;
    }
}

export const validationMessages = {
    valid: '',
    testFailed: 'Failed to check repository access, please try again.',
    nameIsRequired: 'Name is required.',
    unableToConnect: 'Unable to connect to this repo. Create an empty environment.',
    invalidGitUrl: 'We are unable to clone this repository automatically.',
    noAccess: 'You do not have access to this repo.',
    privateRepoNoAuth:
        'Repository doesn’t appear to exist. If it’s private, then you’ll need to authenticate.',
};

enum ValidationState {
    Initial,
    Validating,
    Valid,
    Invalid,
}

type TextFieldState = {
    value: string;
    validation: ValidationState;
    isRequired: boolean;
};

type NumberFieldState = {
    value: number;
    validation: ValidationState;
};

export interface CreateEnvironmentPanelProps {
    defaultName?: string | null;
    defaultRepo?: string | null;

    defaultDotfilesRepository?: string | null;
    defaultDotfilesInstallCommand?: string | null;
    defaultDotfilesTarget?: string | null;

    gitHubAccessToken: string | null;

    selectedPlanId: string | null;
    selectedPlanLocation: string | null;

    autoShutdownDelayMinutes: number;

    storeGitHubCredentials: (accessToken: string) => void;
    hidePanel: () => void;
    onCreateEnvironment: (environmentInfo: CreateEnvironmentParams) => void;
}

interface FormFields {
    friendlyName: TextFieldState;
    gitRepositoryUrl: TextFieldState;
    dotfilesRepository: TextFieldState;
    dotfilesInstallCommand: TextFieldState;
    dotfilesTargetPath: TextFieldState;
    autoShutdownDelayMinutes: NumberFieldState;
}

interface CreateEnvironmentPanelState extends FormFields {
    shouldTryToAuthenticateForRepo: boolean;
    shouldTryToAuthenticateForDotfiles: boolean;
    authenticationErrorMessage: string | undefined;
    authenticationAttempt?: GithubAuthenticationAttempt;
}

const initialFormState: CreateEnvironmentPanelState = {
    friendlyName: { value: '', validation: ValidationState.Initial, isRequired: true },
    gitRepositoryUrl: { value: '', validation: ValidationState.Valid, isRequired: false },
    dotfilesRepository: { value: '', validation: ValidationState.Valid, isRequired: false },
    dotfilesInstallCommand: { value: '', validation: ValidationState.Valid, isRequired: false },
    dotfilesTargetPath: { value: '', validation: ValidationState.Valid, isRequired: false },
    autoShutdownDelayMinutes: { value: 30, validation: ValidationState.Valid },
    shouldTryToAuthenticateForRepo: false,
    shouldTryToAuthenticateForDotfiles: false,
    authenticationErrorMessage: undefined,
    authenticationAttempt: undefined,
};

type Fields = keyof FormFields;

function formToEnvironmentParams(planId: string, planLocation: string, fields: FormFields): CreateEnvironmentParams {
    return {
        planId,
        location: planLocation,
        friendlyName: fields.friendlyName.value,
        gitRepositoryUrl: normalizeGitUrl(fields.gitRepositoryUrl.value),
        dotfilesRepository: normalizeGitUrl(fields.dotfilesRepository.value),
        dotfilesTargetPath: fields.dotfilesTargetPath.value,
        dotfilesInstallCommand: fields.dotfilesInstallCommand.value,
        autoShutdownDelayMinutes: fields.autoShutdownDelayMinutes.value,
    };
}

export const defaultAutoShutdownDelayMinutes: number = 30;
const autoShutdownOptions: IDropdownOption[] = [
    { key: 5, text: '5 Minutes' },
    { key: 30, text: '30 Minutes' },
    { key: 120, text: '2 Hours' },
    { key: 0, text: 'Never' },
];

export class CreateEnvironmentPanelView extends Component<
    CreateEnvironmentPanelProps,
    CreateEnvironmentPanelState
> {
    public constructor(props: CreateEnvironmentPanelProps) {
        super(props);

        this.state = {
            ...initialFormState,
            friendlyName: {
                ...initialFormState.friendlyName,
                value: props.defaultName || '',
            },
            gitRepositoryUrl: {
                ...initialFormState.gitRepositoryUrl,
                value: props.defaultRepo || '',
            },

            dotfilesRepository: {
                ...initialFormState.dotfilesRepository,
                value: props.defaultDotfilesRepository || '',
            },
            dotfilesTargetPath: {
                ...initialFormState.dotfilesTargetPath,
                value: props.defaultDotfilesTarget || '',
            },
            dotfilesInstallCommand: {
                ...initialFormState.dotfilesInstallCommand,
                value: props.defaultDotfilesInstallCommand || '',
            },
            autoShutdownDelayMinutes: {
                ...initialFormState.autoShutdownDelayMinutes,
                value: defaultAutoShutdownDelayMinutes,
            },
        };
    }

    componentWillUnmount() {
        if (this.state.authenticationAttempt) {
            this.state.authenticationAttempt.dispose();
        }
    }

    render() {
        return (
            <Panel
                isOpen={true}
                type={PanelType.smallFixedFar}
                isFooterAtBottom={true}
                onKeyDown={this.onKeyDownPanel}
                onDismiss={this.dismissPanel}
                headerText='Create Environment'
                closeButtonAriaLabel='Close'
                onRenderFooterContent={this.onRenderFooterContent}
            >
                <Stack tokens={{ childrenGap: 'l1' }}>
                    <Stack tokens={{ childrenGap: 4 }}>
                        <TextField
                            label='Environment Name'
                            placeholder='environmentNameExample'
                            onKeyDown={this.submitForm}
                            value={this.state.friendlyName.value}
                            iconProps={getValidationIcon(this.state.friendlyName)}
                            onChange={this.onChangeFriendlyName}
                            onGetErrorMessage={this.onGetErrorMessageFriendlyName}
                            onNotifyValidationResult={this.onNotifyValidationResultFriendlyName}
                            validateOnLoad={!!this.props.defaultName}
                            autoFocus
                            required
                        />
                        <TextField
                            label='Git Repository'
                            placeholder='vsls-contrib/guestbook'
                            onKeyDown={this.submitForm}
                            value={this.state.gitRepositoryUrl.value}
                            iconProps={getValidationIcon(this.state.gitRepositoryUrl)}
                            onChange={this.onChangeGitRepositoryUrl}
                            onGetErrorMessage={this.onGetErrorMessageGitRepo}
                            onNotifyValidationResult={this.onNotifyValidationResultGitRepositoryUrl}
                            validateOnLoad={!!this.props.defaultRepo}
                        />
                        <Dropdown
                            label='Suspend idle environment after...'
                            options={autoShutdownOptions}
                            onChange={this.onChangeAutoShutdownDelayMinutes}
                            selectedKey={this.state.autoShutdownDelayMinutes.value}
                        />
                    </Stack>

                    <Collapsible tokens={{ childrenGap: 4 }} title={'Dotfiles (optional)'}>
                        <TextField
                            autoFocus
                            label='Dotfiles Repository'
                            placeholder='e.g. Org/Repo or https://github.com/Org/Repo.git'
                            onKeyDown={this.submitForm}
                            value={this.state.dotfilesRepository.value}
                            iconProps={getValidationIcon(this.state.dotfilesRepository)}
                            onChange={this.onChangeDotfilesRepository}
                            onGetErrorMessage={this.onGetErrorMessageGitRepo}
                            onNotifyValidationResult={
                                this.onNotifyValidationResultDotfilesRepository
                            }
                            validateOnLoad={false}
                        />
                        <TextField
                            label='Dotfiles Install Command'
                            placeholder='./install.sh'
                            onKeyDown={this.submitForm}
                            value={this.state.dotfilesInstallCommand.value}
                            iconProps={getValidationIcon(this.state.dotfilesInstallCommand)}
                            onChange={this.onChangeDotfilesInstallCommand}
                            validateOnLoad={false}
                        />
                        <TextField
                            label='Dotfiles Target Path'
                            placeholder='~/dotfiles <optional>'
                            onKeyDown={this.submitForm}
                            value={this.state.dotfilesTargetPath.value}
                            iconProps={getValidationIcon(this.state.dotfilesTargetPath)}
                            onChange={this.onChangeDotfilesTargetPath}
                            validateOnLoad={false}
                        />
                    </Collapsible>
                </Stack>
            </Panel>
        );
    }

    private onRenderFooterContent = () => {
        let authStatusMessage;
        if (this.state.authenticationErrorMessage) {
            authStatusMessage = (
                <div className='create-environment-panel__auth'>
                    <p>{this.state.authenticationErrorMessage}</p>;
                </div>
            );
        } else if (!this.isCurrentStateValid() && !!this.state.authenticationAttempt) {
            authStatusMessage = (
                <div className='create-environment-panel__auth'>
                    <p>
                        We've opened a new tab for you to grant permission to the specified GitHub
                        repositories.
                    </p>

                    <p>
                        In case you closed it before finishing or it didn't appear at all,{' '}
                        <Link
                            href={this.state.authenticationAttempt.url}
                            target={this.state.authenticationAttempt.target}
                        >
                            reopen the tab.
                        </Link>
                    </p>
                </div>
            );
        } else {
            authStatusMessage = null;
        }

        const creationDisabled =
            (!this.isCurrentStateValid() &&
                !this.state.shouldTryToAuthenticateForRepo &&
                !this.state.shouldTryToAuthenticateForDotfiles) ||
            (!this.isCurrentStateValid() && !!this.state.authenticationAttempt);

        const label =
            this.state.shouldTryToAuthenticateForRepo ||
            this.state.shouldTryToAuthenticateForDotfiles
                ? 'Auth & Create'
                : 'Create';

        return (
            <Fragment>
                {authStatusMessage}
                <PrimaryButton
                    onClick={this.createEnvironment}
                    style={{ marginRight: '.8rem' }}
                    disabled={creationDisabled}
                >
                    {label}
                </PrimaryButton>
                <DefaultButton style={{ marginRight: '.8rem' }} onClick={this.clearForm}>
                    Cancel
                </DefaultButton>
            </Fragment>
        );
    };

    private onKeyDownPanel: ((event: KeyboardEvent<any>) => void) | undefined = (event) => {
        if (event.keyCode === KeyCodes.escape) {
            this.dismissPanel();
        }
    };

    private submitForm = async (event: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        if (event.keyCode === KeyCodes.enter) {
            await this.createEnvironment(event);
        }
    };

    private dismissPanel = () => {
        this.clearForm();
    };

    private clearForm = () => {
        this.setState(() => ({
            ...initialFormState,
        }));

        this.props.hidePanel();
    };

    private isCurrentStateValid() {
        return (
            this.state.friendlyName.validation === ValidationState.Valid &&
            this.state.gitRepositoryUrl.validation === ValidationState.Valid &&
            this.state.dotfilesRepository.validation === ValidationState.Valid &&
            this.state.dotfilesInstallCommand.validation === ValidationState.Valid &&
            this.state.dotfilesTargetPath.validation === ValidationState.Valid
        );
    }

    private createEnvironment = async (event: SyntheticEvent<any, any>) => {
        event.persist();

        if (
            !this.props.gitHubAccessToken &&
            (this.state.shouldTryToAuthenticateForDotfiles ||
                this.state.shouldTryToAuthenticateForRepo)
        ) {
            try {
                let authenticationAttempt;
                if (this.state.authenticationAttempt) {
                    authenticationAttempt = this.state.authenticationAttempt;
                } else {
                    authenticationAttempt = new GithubAuthenticationAttempt();
                    this.setState({
                        authenticationAttempt,
                    });
                }

                const accessToken = await authenticationAttempt.authenticate();

                if (!accessToken) {
                    throw new Error('Failed to authenticate against GitHub.');
                }

                this.props.storeGitHubCredentials(accessToken);

                const validationMessage = await validateGitRepository(
                    this.state.gitRepositoryUrl.value,
                    this.props.gitHubAccessToken
                );
                if (validationMessage !== validationMessages.valid) {
                    throw new Error(validationMessages.noAccess);
                }

                this.props.onCreateEnvironment(this.getEnvCreationParams());
                return;
            } catch (err) {
                this.setTextValidationState(
                    'gitRepositoryUrl',
                    this.state.gitRepositoryUrl.value,
                    ValidationState.Invalid
                );
                this.setState({
                    shouldTryToAuthenticateForRepo: false,
                    shouldTryToAuthenticateForDotfiles: false,
                    authenticationErrorMessage: undefined,
                    authenticationAttempt: undefined,
                });

                event.preventDefault();
                event.stopPropagation();
                return;
            }
        }

        if (!this.isCurrentStateValid()) {
            event.preventDefault();
            event.stopPropagation();

            return;
        }

        this.props.onCreateEnvironment(this.getEnvCreationParams());
        this.clearForm();
    };

    private getEnvCreationParams() {
        const { selectedPlanId, selectedPlanLocation } = this.props;

        if (!selectedPlanId || !selectedPlanLocation) {
            throw new Error('No plan selected.');
        }

        const envParams = formToEnvironmentParams(selectedPlanId, selectedPlanLocation, this.state);
        return envParams;
    }

    private setTextValidationState(
        field: Fields,
        value: string,
        validation: ValidationState,
        callback?: () => void
    ) {
        this.setState((previousState) => {
            const previousFieldState = previousState[field];

            return {
                ...previousState,
                [field]: {
                    ...previousFieldState,
                    value,
                    validation,
                },
            };
        }, callback);
    }

    private setNumberValidationState(
        field: Fields,
        value: number,
        validation: ValidationState,
        callback?: () => void
    ) {
        this.setState((previousState) => {
            const previousFieldState = previousState[field];

            return {
                ...previousState,
                [field]: {
                    ...previousFieldState,
                    value,
                    validation,
                },
            };
        }, callback);
    }

    private onChangeFriendlyName = (_event: unknown, value = '') => {
        this.setTextValidationState('friendlyName', value, ValidationState.Validating);
    };

    private onGetErrorMessageFriendlyName = (value: string) => {
        if (value.trim().length === 0) {
            return validationMessages.nameIsRequired;
        }
    };

    private onNotifyValidationResultFriendlyName = (
        errorMessage: string | ReactElement,
        value = ''
    ) => {
        this.setTextValidationState(
            'friendlyName',
            value,
            errorMessage ? ValidationState.Invalid : ValidationState.Valid
        );
    };

    private onChangeGitRepositoryUrl = (_event: unknown, value = '') => {
        this.setState({
            shouldTryToAuthenticateForRepo: false,
            authenticationErrorMessage: undefined,
        });

        this.setTextValidationState('gitRepositoryUrl', value, ValidationState.Validating);
    };

    private onGetErrorMessageGitRepo = async (value: string) => {
        return await validateGitRepository(value, this.props.gitHubAccessToken);
    };

    private onNotifyValidationResultGitRepositoryUrl = (
        errorMessage: string | ReactElement,
        value = ''
    ) => {
        this.setTextValidationState(
            'gitRepositoryUrl',
            value,
            errorMessage ? ValidationState.Invalid : ValidationState.Valid
        );

        if (errorMessage) {
            this.setState({
                shouldTryToAuthenticateForRepo:
                    errorMessage === validationMessages.privateRepoNoAuth,
            });
        }
    };

    private onChangeDotfilesRepository = (_event: unknown, value = '') => {
        this.setState({
            shouldTryToAuthenticateForDotfiles: false,
            authenticationErrorMessage: undefined,
        });

        this.setTextValidationState('dotfilesRepository', value, ValidationState.Validating);
    };

    private onNotifyValidationResultDotfilesRepository = (
        errorMessage: string | ReactElement,
        value = ''
    ) => {
        this.setTextValidationState(
            'dotfilesRepository',
            value,
            errorMessage ? ValidationState.Invalid : ValidationState.Valid
        );

        if (errorMessage) {
            this.setState({
                shouldTryToAuthenticateForDotfiles:
                    errorMessage === validationMessages.privateRepoNoAuth,
            });
        }
    };

    private onChangeDotfilesInstallCommand = (_event: unknown, value = '') => {
        this.setTextValidationState('dotfilesInstallCommand', value, ValidationState.Valid);
    };

    private onChangeDotfilesTargetPath = (_event: unknown, value = '') => {
        this.setTextValidationState('dotfilesTargetPath', value, ValidationState.Valid);
    };

    private onChangeAutoShutdownDelayMinutes = (_event: unknown, option?: IDropdownOption) => {
        if (option) {
            if (typeof option.key !== 'number') {
                throw new Error('NotImplemented');
            }
            this.setNumberValidationState(
                'autoShutdownDelayMinutes',
                option.key,
                ValidationState.Valid
            );
        }
    };
}

export const CreateEnvironmentPanel = connect(
    ({ githubAuthentication: { gitHubAccessToken }, plans }: ApplicationState) => {
        const { selectedPlan } = plans;

        const selectedPlanId = (selectedPlan)
            ? selectedPlan.id
            : null

        const selectedPlanLocation = (selectedPlan)
            ? selectedPlan.location
            : null

        return {
            gitHubAccessToken,
            selectedPlanId,
            selectedPlanLocation
        }
    },
    {
        storeGitHubCredentials,
    }
)(CreateEnvironmentPanelView);
