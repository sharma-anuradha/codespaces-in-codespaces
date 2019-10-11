import React, { Component, KeyboardEvent, SyntheticEvent, ReactElement, Fragment } from 'react';
import { connect } from 'react-redux';

import { PrimaryButton, DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { KeyCodes } from '@uifabric/utilities';

import { useWebClient } from '../../actions/middleware/useWebClient';
import { createEnvironment } from '../../actions/createEnvironment';
import { storeGitHubCredentials } from '../../actions/getGitHubCredentials';
import { ApplicationState } from '../../reducers/rootReducer';
import { GithubAuthenticationAttempt } from '../../services/gitHubAuthenticationService';
import { Link } from 'office-ui-fabric-react/lib/components/Link';
import { Collapsible } from '../collapsible/collapsible';

import './create-environment-panel.css';

type CreateEnvironmentParams = Parameters<typeof createEnvironment>[0];

function normalizeGitUrl(url: string): string | undefined {
    let result = url.trim();
    if (result.length === 0) {
        return undefined;
    }

    // GitHub allows organization names to contain alphanumeric characters + hyphens. They cannot start or end in hyphen.
    // For repository names they turn all non-hyphen symbols to hyphens and allow underscores and hyphens in the beginning and end.
    const shortGithubUrlRegex = /^([a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]|[a-zA-Z0-9]+)\/[_\-a-zA-Z0-9]+$/;
    if (shortGithubUrlRegex.test(result)) {
        return `https://github.com/${result}`;
    }

    // If we don't need to normalize to full form or no-data form, it's a valid full git url.
    return result;
}

function getGitProvider(repo: string): SupportedGitServices {
    const normalizedRepo = normalizeGitUrl(repo);

    if (!normalizedRepo) {
        return SupportedGitServices.Unknown;
    }

    const providers: [SupportedGitServices, RegExp][] = [
        [
            SupportedGitServices.BitBucket,
            /^(https?:\/\/)?(www\.)?(bitbucket\.org)\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)$/,
        ],
        [
            SupportedGitServices.GitLab,
            /^(https?:\/\/)?(www\.)?(gitlab\.com)\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)$/,
        ],
        [
            SupportedGitServices.GitHub,
            /^(https?:\/\/)?(www\.)?(github\.com)\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)$/,
        ],
    ];

    for (const [provider, regex] of providers) {
        if (regex.test(normalizedRepo)) {
            return provider;
        }
    }

    return SupportedGitServices.Unknown;
}

function gitHubUrlToGithubApiUrl(fullGitHubUrl: string) {
    const gitHubUrl = new URL(fullGitHubUrl);
    const url = new URL(`/repos${gitHubUrl.pathname}`, 'https://api.github.com/');

    return url.toString();
}

async function pingUrl(url: string, bearerToken?: string): Promise<boolean> {
    const webClient = useWebClient();

    const headers: Headers = new Headers();
    if (bearerToken) {
        headers.set('Authorization', `Bearer ${bearerToken}`);
    }

    try {
        await webClient.request(
            url.toString(),
            { headers },
            { skipParsingResponse: true, requiresAuthentication: false }
        );
        return true;
    } catch {
        return false;
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

const validationErrorMessages = {
    nameIsRequired: 'Name is required.',
    unableToConnect: 'Unable to connect to this repo. Create an empty environment.',
    invalidGitUrl: 'We are not able to clone this repository into the environment automatically.',
    noAccess: 'You do not have access to this repo.',
    privateRepoNoAuth: 'Private GitHub repo detected.',
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

enum SupportedGitServices {
    Unknown,
    GitHub = 'github.com',
    BitBucket = 'bitbucket.org',
    GitLab = 'gitlab.com',
}

export interface CreateEnvironmentPanelProps {
    defaultName?: string | null;
    defaultRepo?: string | null;

    defaultDotfilesRepository?: string | null;
    defaultDotfilesInstallCommand?: string | null;
    defaultDotfilesTarget?: string | null;

    gitHubAccessToken: string | null;

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
    shouldTryToAuthenticateForRepo: false,
    shouldTryToAuthenticateForDotfiles: false,
    authenticationErrorMessage: undefined,
    authenticationAttempt: undefined,
};

type Fields = keyof FormFields;

function formToEnvironmentParams(fields: FormFields): CreateEnvironmentParams {
    return {
        friendlyName: fields.friendlyName.value,
        gitRepositoryUrl: normalizeGitUrl(fields.gitRepositoryUrl.value),
        dotfilesRepository: normalizeGitUrl(fields.dotfilesRepository.value),
        dotfilesTargetPath: fields.dotfilesTargetPath.value,
        dotfilesInstallCommand: fields.dotfilesInstallCommand.value,
    };
}

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
            (!this.isCurrentStateValid() && !this.state.shouldTryToAuthenticateForRepo) ||
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

                const maybeGitUrl = normalizeGitUrl(this.state.gitRepositoryUrl.value);
                if (!maybeGitUrl) {
                    throw new Error('Internal error: state mismatch.');
                }

                const pingableUrl = gitHubUrlToGithubApiUrl(maybeGitUrl);
                const isGitHubRepositoryAccessible = await pingUrl(pingableUrl, accessToken);
                if (!isGitHubRepositoryAccessible) {
                    throw new Error(validationErrorMessages.noAccess);
                }

                this.props.onCreateEnvironment(formToEnvironmentParams(this.state));

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

        this.props.onCreateEnvironment(formToEnvironmentParams(this.state));
        this.clearForm();
    };

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

    private onChangeFriendlyName = (_event: unknown, value = '') => {
        this.setTextValidationState('friendlyName', value, ValidationState.Validating);
    };

    private onGetErrorMessageFriendlyName = (value: string) => {
        if (value.trim().length === 0) {
            return validationErrorMessages.nameIsRequired;
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
        const valid = '';
        const maybeGitUrl = normalizeGitUrl(value);

        if (!maybeGitUrl) {
            return valid;
        }

        const gitServiceProvider = getGitProvider(maybeGitUrl);
        switch (gitServiceProvider) {
            case SupportedGitServices.GitHub:
                const pingableUrl = gitHubUrlToGithubApiUrl(maybeGitUrl);
                if (this.props.gitHubAccessToken) {
                    const isAccessible = await pingUrl(pingableUrl, this.props.gitHubAccessToken);
                    if (!isAccessible) {
                        return validationErrorMessages.noAccess;
                    }
                } else {
                    const isAccessible = await pingUrl(pingableUrl);
                    if (!isAccessible) {
                        return validationErrorMessages.privateRepoNoAuth;
                    }
                }

                return valid;

            case SupportedGitServices.BitBucket:
            case SupportedGitServices.GitLab:
                return (await pingUrl(maybeGitUrl))
                    ? valid
                    : validationErrorMessages.unableToConnect;

            default:
                return validationErrorMessages.invalidGitUrl;
        }
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
                    errorMessage === validationErrorMessages.privateRepoNoAuth,
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
                    errorMessage === validationErrorMessages.privateRepoNoAuth,
            });
        }
    };

    private onChangeDotfilesInstallCommand = (_event: unknown, value = '') => {
        this.setTextValidationState('dotfilesInstallCommand', value, ValidationState.Valid);
    };

    private onChangeDotfilesTargetPath = (_event: unknown, value = '') => {
        this.setTextValidationState('dotfilesTargetPath', value, ValidationState.Valid);
    };
}

export const CreateEnvironmentPanel = connect(
    ({ githubAuthentication: { gitHubAccessToken } }: ApplicationState) => ({
        gitHubAccessToken,
    }),
    {
        storeGitHubCredentials,
    }
)(CreateEnvironmentPanelView);
