import React, { Component, SyntheticEvent, ReactElement } from 'react';
import { connect } from 'react-redux';

import { PrimaryButton, DefaultButton, IconButton } from 'office-ui-fabric-react/lib/Button';
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { TextField, ITextFieldProps } from 'office-ui-fabric-react/lib/TextField';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { KeyCodes, IRenderFunction } from '@uifabric/utilities';

import { isDefined, Signal } from 'vso-client-core';
import { SupportedGitService, getSupportedGitService } from 'vso-ts-agent';

import { Link } from 'office-ui-fabric-react/lib/components/Link';
import {
    Dropdown,
    IDropdownOption,
    IDropdown,
    IDropdownProps,
} from 'office-ui-fabric-react/lib/Dropdown';
import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { ISelectableOption } from 'office-ui-fabric-react/lib/utilities/selectableOption/SelectableOption.types';

import { useWebClient, ServiceResponseError } from '../../actions/middleware/useWebClient';
import { createEnvironment } from '../../actions/createEnvironment';
import { storeGitHubCredentials, getGitHubCredentials } from '../../actions/getGitHubCredentials';
import { storeAzDevCredentials, getAzDevCredentials } from '../../actions/getAzDevCredentials';
import { ApplicationState } from '../../reducers/rootReducer';
import { GithubAuthenticationAttempt } from '../../services/gitHubAuthenticationService';
import { AzDevAuthenticationAttempt } from '../../services/azDevAuthenticationService';
import { ISku } from '../../interfaces/ISku';
import { IPlan } from '../../interfaces/IPlan';
import { ActivePlanInfo } from '../../reducers/plans-reducer';
import { getPlans } from '../../actions/plans-actions';
import { Collapsible } from '../collapsible/collapsible';
import { DropDownWithLoader } from '../dropdown-with-loader/dropdown-with-loader';

import {
    normalizeGitUrl,
    getQueryableUrl,
} from '../../utils/gitUrlNormalization';

import './create-environment-panel.css';
import { Loader } from '../loader/loader';
import { isNotNullOrEmpty } from '../../utils/isNotNullOrEmpty';
import { getSkuSpecLabel } from '../../utils/environmentUtils';
import { IAuthenticationAttempt } from '../../services/authenticationServiceBase';
import { WithTranslation, withTranslation } from 'react-i18next';
import { injectMessageParameters } from '../../utils/injectMessageParameters';
import { TFunction } from 'i18next';


type CreateEnvironmentParams = Parameters<typeof createEnvironment>[0];

const SKU_SHOW_PRICING_KEY = 'show-pricing';
const SKU_PRICING_LABEL = 'Show pricing information...';
const SKU_PRICING_URL = 'https://aka.ms/vso-pricing';

const USE_TRUSTWORTHY_REPO_LABEL_TEXT = 'You should only use ';
const USE_TRUSTWORTHY_REPO_LABEL_LINK = 'repositories you trust';
const USE_TRUSTWORTHY_REPO_URL = 'https://aka.ms/vso-trusted-repos';

export enum validationMessagesKeys {
    valid,
    testFailed,
    nameIsRequired,
    nameIsTooLong,
    nameIsInvalid,
    unableToConnect,
    invalidGitUrl,
    noAccess,
    noAccessDotFiles,
    privateRepoNoAuth,
    noPlanSelected,
    noSkusAvailable,
}

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
    azDevAccessToken: string | null = null,
    required = false
): Promise<validationMessagesKeys> {
    const valid = '';
    const maybeGitUrl = normalizeGitUrl(maybeRepository);
    if (!required && !maybeRepository) {
        return validationMessagesKeys.valid;
    }
    if (!maybeGitUrl) {
        return validationMessagesKeys.invalidGitUrl;
    }

    const gitServiceProvider = getSupportedGitService(maybeGitUrl);
    const queryableUrl = getQueryableUrl(maybeGitUrl);
    if (!queryableUrl) {
        return validationMessagesKeys.invalidGitUrl;
    }

    if (gitServiceProvider === SupportedGitService.GitHub && gitHubAccessToken) {
        const isAccessible = await queryGitService(queryableUrl, gitHubAccessToken);
        if (!isAccessible) {
            return validationMessagesKeys.noAccess;
        } else {
            return validationMessagesKeys.valid;
        }
    } else if (gitServiceProvider === SupportedGitService.GitHub) {
        try {
            const isAccessible = await queryGitService(queryableUrl);
            if (!isAccessible || localStorage.getItem('vscs-showserverless') === 'true') {
                return validationMessagesKeys.privateRepoNoAuth;
            } else {
                return validationMessagesKeys.valid;
            }
        } catch {
            return validationMessagesKeys.testFailed;
        }
    } else if (gitServiceProvider === SupportedGitService.AzureDevOps && azDevAccessToken) {
        // ToDo: Check to see if AzDevOpsRepo is a valid Repo
        return validationMessagesKeys.valid;
    } else if (gitServiceProvider === SupportedGitService.AzureDevOps) {
        // ToDo: Check if AzureDevOps repo is a public repo. https://docs.microsoft.com/en-us/azure/devops/organizations/public/make-project-public?view=azure-devops
        return validationMessagesKeys.privateRepoNoAuth;
    } else {
        try {
            if (await queryGitService(queryableUrl)) {
                return validationMessagesKeys.valid;
            } else {
                return validationMessagesKeys.unableToConnect;
            }
        } catch {
            return validationMessagesKeys.testFailed;
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

function normalizeOptionalValue(value: string): string | undefined {
    value = value.trim();

    if (!value) {
        return undefined;
    }

    return value;
}

export function getValidationMessage(key: validationMessagesKeys, translationFunc: (key: string) => string) {
    switch (key) {
        case validationMessagesKeys.valid: return translationFunc('');
        case validationMessagesKeys.testFailed: return translationFunc('testFailed');
        case validationMessagesKeys.nameIsRequired: return translationFunc('nameIsRequired');
        case validationMessagesKeys.nameIsTooLong: return translationFunc('nameIsTooLong');
        case validationMessagesKeys.nameIsInvalid: return translationFunc('nameIsInvalid');
        case validationMessagesKeys.unableToConnect: return translationFunc('unableToConnect');
        case validationMessagesKeys.invalidGitUrl: return translationFunc('invalidGitUrl');
        case validationMessagesKeys.noAccess: return translationFunc('noAccess');
        case validationMessagesKeys.noAccessDotFiles: return translationFunc('noAccessDotFiles');
        case validationMessagesKeys.privateRepoNoAuth: return translationFunc('privateRepoNoAuth');
        case validationMessagesKeys.noPlanSelected: return translationFunc('noPlanSelected');
        case validationMessagesKeys.noSkusAvailable: return translationFunc('noSkusAvailable');
    }
}

enum ValidationState {
    Initial,
    Validating,
    Valid,
    Invalid,
}

type TextFieldState = {
    value: string;
    validation: ValidationState;
    errorMessage?: string | undefined;
    style?: string | undefined;
    isRequired: boolean;
};

type NumberFieldState = {
    value: number;
    validation: ValidationState;
};

export interface CreateEnvironmentPanelProps extends WithTranslation {
    defaultName?: string | null;
    defaultRepo?: string | null;

    defaultDotfilesRepository?: string | null;
    defaultDotfilesInstallCommand?: string | null;
    defaultDotfilesTarget?: string | null;

    gitHubAccessToken: string | null;
    azDevAccessToken: string | null;

    selectedPlan: ActivePlanInfo | null;
    isPlanLoadingFinished?: boolean | null;

    autoShutdownDelayMinutes: number;

    defaultSkuName?: string | null;

    errorMessage: string | undefined;
    hideErrorMessage: () => void;

    storeGitHubCredentials: (accessToken: string) => void;
    storeAzDevCredentials: (accessToken: string) => void;
    hidePanel: () => void;
    onCreateEnvironment: (environmentInfo: CreateEnvironmentParams) => Promise<void>;
}

interface FormFields {
    friendlyName: TextFieldState;
    gitRepositoryUrl: TextFieldState;
    dotfilesRepository: TextFieldState;
    dotfilesInstallCommand: TextFieldState;
    dotfilesTargetPath: TextFieldState;
    autoShutdownDelayMinutes: NumberFieldState;
    skuName: TextFieldState;
}

interface CreateEnvironmentPanelState extends FormFields {
    shouldTryToAuthenticateForRepo: boolean;
    shouldTryToAuthenticateForDotfiles: boolean;
    authenticationAttempt?: IAuthenticationAttempt;
    isCreatingEnvironment?: boolean;
}

const initialFormState: CreateEnvironmentPanelState = {
    friendlyName: {
        value: '',
        validation: ValidationState.Initial,
        errorMessage: undefined,
        style: undefined,
        isRequired: true,
    },
    gitRepositoryUrl: {
        value: '',
        validation: ValidationState.Valid,
        errorMessage: undefined,
        style: undefined,
        isRequired: false,
    },
    dotfilesRepository: {
        value: '',
        validation: ValidationState.Valid,
        errorMessage: undefined,
        style: undefined,
        isRequired: false,
    },
    dotfilesInstallCommand: { value: '', validation: ValidationState.Valid, isRequired: false },
    dotfilesTargetPath: { value: '', validation: ValidationState.Valid, isRequired: false },
    autoShutdownDelayMinutes: { value: 30, validation: ValidationState.Valid },
    skuName: { value: '', validation: ValidationState.Initial, isRequired: true },
    shouldTryToAuthenticateForRepo: false,
    shouldTryToAuthenticateForDotfiles: false,
    authenticationAttempt: undefined,
};

type Fields = keyof FormFields;

function formToEnvironmentParams(plan: IPlan, fields: FormFields): CreateEnvironmentParams {
    return {
        planId: plan.id,
        location: plan.location,
        friendlyName: fields.friendlyName.value,
        gitRepositoryUrl: normalizeGitUrl(fields.gitRepositoryUrl.value),
        dotfilesRepository: normalizeGitUrl(fields.dotfilesRepository.value),
        dotfilesTargetPath: normalizeOptionalValue(fields.dotfilesTargetPath.value),
        dotfilesInstallCommand: normalizeOptionalValue(fields.dotfilesInstallCommand.value),
        autoShutdownDelayMinutes: fields.autoShutdownDelayMinutes.value,
        skuName: fields.skuName.value,
    };
}

export const defaultAutoShutdownDelayMinutes: number = 30;
function autoShutdownOptions(translationFunc: TFunction): IDropdownOption[] {
    return [
        { key: 5, text: injectMessageParameters(translationFunc('autoShutdownMinutes'), '5') },
        { key: 30, text: injectMessageParameters(translationFunc('autoShutdownMinutes'), '30') },
        { key: 60, text: injectMessageParameters(translationFunc('autoShutdownHour'), '1') },
        { key: 120, text: injectMessageParameters(translationFunc('autoShutdownHours'), '2') },
    ]
}

const openExternalUrl = (url: string) => {
    window.open(url, '_blank');
};

const errorTextfieldClassname = 'create-environment-panel__errorTextField';

function createLabelRenderCallback(title: string, onClickUrl: string, customLabel?: string) {
    return (props: { label: string }) => {
        return (
            <Stack id={title} horizontal verticalAlign='center'>
                <span style={{ fontWeight: 600 }}>{props.label || customLabel}</span>
                <IconButton
                    iconProps={{ iconName: 'Info' }}
                    title={title}
                    ariaLabel={title}
                    styles={{ root: { marginBottom: -3 } }}
                    onClick={openExternalUrl.bind(null, onClickUrl)}
                />
            </Stack>
        );
    };
}

export class CreateEnvironmentPanelView extends Component<
    CreateEnvironmentPanelProps,
    CreateEnvironmentPanelState
> {
    private creationInProgress?: Signal<void>;

    public constructor(props: CreateEnvironmentPanelProps) {
        super(props);

        const availableSkus = this.getAvailableSkus();

        const defaultSkuSelection =
            props.defaultSkuName ||
            (availableSkus && availableSkus.length && availableSkus[0].name) ||
            '';

        // Workaround for DropDown not having validateOnLoad
        const isInitialSkuValid = this.isSkuNameValid(defaultSkuSelection, availableSkus);
        const { t: translation } = props;

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
            skuName: {
                ...initialFormState.skuName,
                value: defaultSkuSelection,
                validation: isInitialSkuValid ? ValidationState.Valid : ValidationState.Initial,
            },
        };

        if (!availableSkus) {
            // Forces a refresh of the plan list and selects a default one
            getPlans();
        }
    }

    componentDidMount() {
        document.addEventListener('keydown', this.onKeyDownPanel);
    }

    componentWillUnmount() {
        if (this.state.authenticationAttempt) {
            this.state.authenticationAttempt.dispose();
        }
        document.removeEventListener('keydown', this.onKeyDownPanel);
    }

    render() {
        const { t: translation } = this.props;
        return (
            <Panel
                isOpen={true}
                type={PanelType.smallFixedFar}
                isFooterAtBottom={true}
                onDismiss={this.dismissPanel}
                headerText={translation('createCodespace')}
                closeButtonAriaLabel={translation('close')}
                onRenderFooterContent={this.onRenderFooterContent}
            >
                {this.renderOverlay()}

                {this.renderCreateEnvironmentInputs()}
            </Panel>
        );
    }

    private renderOverlay() {
        const { isCreatingEnvironment } = this.state;
        const { t: translation} = this.props;

        if (!isCreatingEnvironment) {
            return null;
        }

        return (
            <div className='create-environment-panel__overlay'>
                <Loader message={translation('creatingCodespace')} translation={translation} />
            </div>
        );
    }

    private renderCreateEnvironmentInputs() {
        const { t: translation} = this.props;
        const errorMessageBar = isDefined(this.state.friendlyName.errorMessage) ? (
            <MessageBar messageBarType={MessageBarType.error} isMultiline={true}>
                {this.state.friendlyName.errorMessage}
            </MessageBar>
        ) : null;
        const repoMessageBar = isDefined(this.state.gitRepositoryUrl.errorMessage) ? (
            <MessageBar messageBarType={MessageBarType.error} isMultiline={true} truncated={true}>
                {this.state.gitRepositoryUrl.errorMessage}
            </MessageBar>
        ) : null;
        const dotfilesRepoMessageBar = isDefined(this.state.dotfilesRepository.errorMessage) ? (
            <MessageBar messageBarType={MessageBarType.error} isMultiline={true} truncated={true}>
                {this.state.dotfilesRepository.errorMessage}
            </MessageBar>
        ) : null;
        const selfHostedMessageBar = (
            <MessageBar messageBarType={MessageBarType.info} isMultiline={true} truncated={true}>
                {translation('selfHostedMessage')}
                <Link href='https://aka.ms/vso-self-hosted' target='_blank'>
                    {translation('moreInfo')}
                </Link>
            </MessageBar>
        );

        return (
            <Stack tokens={{ childrenGap: 'l1' }}>
                <Stack>
                    {selfHostedMessageBar}
                    {errorMessageBar}
                    {repoMessageBar}
                    {dotfilesRepoMessageBar}
                </Stack>

                {this.renderEnvironmentCreation()}

                {this.renderDotFiles()}
            </Stack>
        );
    }

    private renderEnvironmentCreation() {
        const { t: translation } = this.props;
        const onSuspendRenderLabel = createLabelRenderCallback(
            translation('viewSuspendBehaviorDetails'),
            'https://aka.ms/vso-docs/how-to/suspend'
        );

        return (
            <Stack tokens={{ childrenGap: 4 }}>
                <TextField
                    label={translation('codespaceName')}
                    ariaLabel='Codespace Name'
                    className={this.state.friendlyName.style}
                    placeholder=''
                    onKeyDown={this.submitForm}
                    value={this.state.friendlyName.value}
                    iconProps={getValidationIcon(this.state.friendlyName)}
                    onChange={this.onChangeFriendlyName}
                    onGetErrorMessage={this.onGetErrorMessageFriendlyName}
                    onNotifyValidationResult={this.onNotifyValidationResultFriendlyName}
                    validateOnLoad={isDefined(this.props.defaultName)}
                    deferredValidationTime={1000}
                    autoFocus
                    required
                />
                <TextField
                    label={translation('gitRepository')}
                    ariaLabel='Git Repository'
                    className={this.state.gitRepositoryUrl.style}
                    placeholder=''
                    onKeyDown={this.submitForm}
                    value={this.state.gitRepositoryUrl.value}
                    iconProps={getValidationIcon(this.state.gitRepositoryUrl)}
                    onChange={this.onChangeGitRepositoryUrl}
                    onGetErrorMessage={this.onGetErrorMessageGitRepo}
                    onNotifyValidationResult={this.onNotifyValidationResultGitRepositoryUrl}
                    validateOnLoad={isDefined(this.props.defaultRepo)}
                    deferredValidationTime={1500}
                />
                {this.renderSkuSelector()}
                <Dropdown
                    label={translation('suspendIdleCodespaceAfter')}
                    ariaLabel='Suspend idle Codespace after...'
                    options={autoShutdownOptions(translation)}
                    onChange={this.onChangeAutoShutdownDelayMinutes}
                    selectedKey={this.state.autoShutdownDelayMinutes.value}
                    onRenderLabel={onSuspendRenderLabel as IRenderFunction<IDropdownProps>}
                />
            </Stack>
        );
    }

    private renderDotFiles() {
        const { t: translation } = this.props;
        const dotFilesDetailsTitle = translation('dotFilesDetails');
        const dotFilesRepositoryTitle = translation('dotFilesRepository');
        const onDotfilesRenderLabel = createLabelRenderCallback(
            dotFilesDetailsTitle,
            'https://aka.ms/vso-docs/reference/personalizing',
            dotFilesRepositoryTitle
        );

        return (
            <Collapsible tokens={{ childrenGap: 4 }} title={`Dotfiles (${translation('optional')})`}>
                <TextField
                    autoFocus
                    ariaLabel={dotFilesRepositoryTitle} // Omitting label due to office-ui's onRenderLabel accessibility conflict
                    className={this.state.dotfilesRepository.style}
                    placeholder=''
                    onKeyDown={this.submitForm}
                    value={this.state.dotfilesRepository.value}
                    iconProps={getValidationIcon(this.state.dotfilesRepository)}
                    onChange={this.onChangeDotfilesRepository}
                    onGetErrorMessage={this.onGetErrorMessageDotfilesRepo}
                    onNotifyValidationResult={this.onNotifyValidationResultDotfilesRepository}
                    validateOnLoad={isDefined(this.state.dotfilesRepository)}
                    deferredValidationTime={1500}
                    onRenderLabel={onDotfilesRenderLabel as IRenderFunction<ITextFieldProps>}
                />
                <TextField
                    label={translation('dotFilesInstallCommand')}
                    ariaLabel='Dotfiles Install Command'
                    placeholder='./install.sh'
                    onKeyDown={this.submitForm}
                    value={this.state.dotfilesInstallCommand.value}
                    iconProps={getValidationIcon(this.state.dotfilesInstallCommand)}
                    onChange={this.onChangeDotfilesInstallCommand}
                    validateOnLoad={false}
                />
                <TextField
                    label={translation('dotFilesTargetPath')}
                    ariaLabel='Dotfiles Target Path'
                    placeholder='~/dotfiles'
                    onKeyDown={this.submitForm}
                    value={this.state.dotfilesTargetPath.value}
                    iconProps={getValidationIcon(this.state.dotfilesTargetPath)}
                    onChange={this.onChangeDotfilesTargetPath}
                    validateOnLoad={false}
                />
            </Collapsible>
        );
    }

    private skuDropdownRef = React.createRef<IDropdown>();

    private renderSkuSelector() {
        const availableSkus = this.getAvailableSkus();
        const { t: translation } = this.props;

        const options: IDropdownOption[] = availableSkus
            ? availableSkus.map((s) => {
                  const text = getSkuSpecLabel(s, translation);
                  return { key: s.name, text };
              })
            : [];

        const errorMessage = this.getSkuNameValidationMessage();

        options.push({
            key: SKU_SHOW_PRICING_KEY,
            text: SKU_PRICING_LABEL,
            data: { icon: 'OpenInNewTab', url: SKU_PRICING_URL },
            ariaLabel: `${SKU_PRICING_LABEL}, ${SKU_PRICING_URL}`,
        });

        const onSkuLabelRender = createLabelRenderCallback(SKU_PRICING_LABEL, SKU_PRICING_URL);
        return (
            <DropDownWithLoader
                componentRef={this.skuDropdownRef}
                label={translation('instanceType')}
                ariaLabel='Instance Type'
                options={options}
                isLoading={!this.props.isPlanLoadingFinished || false}
                loadingMessage={translation('loadingAvailableInstanceTypes')}
                selectedKey={this.state.skuName.value}
                errorMessage={errorMessage}
                disabled={!!errorMessage}
                onChange={this.onChangeSkuName}
                onRenderOption={this.onSkuOptionRender as IRenderFunction<ISelectableOption>}
                onRenderLabel={onSkuLabelRender as IRenderFunction<IDropdownProps>}
                translation={translation}
            />
        );
    }

    private onSkuOptionRender = (option: IDropdownOption) => {
        return (
            <div>
                {option.data && option.data.icon && (
                    <Icon
                        style={{ marginRight: '.8rem' }}
                        iconName={option.data.icon}
                        aria-hidden='true'
                        title={option.data.icon}
                    />
                )}
                <span>
                    {option.data && option.data.url ? (
                        <Link href={option.data.url} target='blank'>
                            {option.text}
                        </Link>
                    ) : (
                        option.text
                    )}
                </span>
            </div>
        );
    };

    private onRenderFooterContent = () => {
        const { t: translation } = this.props;
        let authStatusMessage;
        if (isDefined(this.state.authenticationAttempt)) {
            let authStatusMessageString;
            switch (this.state.authenticationAttempt.gitServiceType) {
                case SupportedGitService.AzureDevOps:
                    authStatusMessageString = translation('azureAuthMessageString');
                    break;
                default:
                    authStatusMessageString = translation('githubAuthMessageString');
            }
            authStatusMessage = (
                <div className='create-environment-panel__auth'>
                    <p>{authStatusMessageString}</p>

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

        let useOnlyTrustworthyReposMessage = null;
        if (this.state.dotfilesRepository.value || this.state.gitRepositoryUrl.value) {
            useOnlyTrustworthyReposMessage = (
                <div className='create-environment-panel__trustworthyReposMessage'>
                    <Icon iconName='ReportHacked' style={{ marginRight: '.8rem' }} />
                    <div>
                        {USE_TRUSTWORTHY_REPO_LABEL_TEXT}
                        <Link target='_blank' href={USE_TRUSTWORTHY_REPO_URL}>
                            {USE_TRUSTWORTHY_REPO_LABEL_LINK}
                        </Link>
                        .
                    </div>
                </div>
            );
        }

        const label =
            this.state.shouldTryToAuthenticateForRepo ||
            this.state.shouldTryToAuthenticateForDotfiles
                ? translation('authAndCreate')
                : translation('create');

        return (
            <Stack tokens={{ childrenGap: 'l1' }}>
                <Stack.Item>{authStatusMessage}</Stack.Item>
                <Stack.Item>{useOnlyTrustworthyReposMessage}</Stack.Item>
                <Stack.Item>
                    <PrimaryButton
                        data-testid='Create Environment'
                        onClick={this.createEnvironment}
                        style={{ marginRight: '.8rem' }}
                        disabled={!this.isCurrentStateValid()}
                    >
                        {label}
                    </PrimaryButton>
                    <DefaultButton style={{ marginRight: '.8rem' }} onClick={this.dismissPanel}>
                        Cancel
                    </DefaultButton>
                </Stack.Item>
            </Stack>
        );
    };

    private onKeyDownPanel = (event: KeyboardEvent) => {
        if (event.keyCode === KeyCodes.escape) {
            this.dismissPanel();
        }
    };

    private submitForm = async (
        event: React.KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>
    ) => {
        if (event.keyCode === KeyCodes.enter) {
            await this.createEnvironment(event);
        }
    };

    private dismissPanel = () => {
        this.clearForm();
        this.props.hidePanel();
    };

    private clearForm = () => {
        this.setState(() => ({
            ...initialFormState,
        }));
    };

    private getAvailableSkus() {
        return this.props.selectedPlan && this.props.selectedPlan.availableSkus;
    }

    private isCurrentStateValid() {
        return (
            this.state.friendlyName.validation === ValidationState.Valid &&
            this.state.gitRepositoryUrl.validation === ValidationState.Valid &&
            this.state.dotfilesRepository.validation === ValidationState.Valid &&
            this.state.dotfilesInstallCommand.validation === ValidationState.Valid &&
            this.state.dotfilesTargetPath.validation === ValidationState.Valid &&
            this.state.skuName.validation === ValidationState.Valid
        );
    }

    private createEnvironment = async (event: SyntheticEvent<any, any>) => {
        if (this.creationInProgress) {
            return this.creationInProgress.promise;
        }

        this.creationInProgress = Signal.from(this.runCreateEnvironment(event));

        await this.creationInProgress.promise;
        this.creationInProgress = undefined;
    };

    private runCreateEnvironment = async (event: SyntheticEvent<any, any>) => {
        event.persist();

        if (
            (!this.props.gitHubAccessToken || !this.props.azDevAccessToken) &&
            (this.state.shouldTryToAuthenticateForDotfiles ||
                this.state.shouldTryToAuthenticateForRepo)
        ) {
            try {
                await this.getGitHubAndAzureDevOpsAccessTokens();

                const validationMessage = await validateGitRepository(
                    this.state.gitRepositoryUrl.value,
                    this.props.gitHubAccessToken,
                    this.props.azDevAccessToken
                );
                if (validationMessage !== validationMessagesKeys.valid) {
                    this.showRepoError();
                    this.onNotifyValidationResultGitRepositoryUrl(
                        getValidationMessage(validationMessagesKeys.noAccess, this.props.t),
                        this.state.gitRepositoryUrl.value
                    );
                }
                const dotfilesValidationMessage = await validateGitRepository(
                    this.state.dotfilesRepository.value,
                    this.props.gitHubAccessToken,
                    this.props.azDevAccessToken
                );
                if (dotfilesValidationMessage !== validationMessagesKeys.valid) {
                    this.showDotfilesError();
                    this.onNotifyValidationResultDotfilesRepository(
                        getValidationMessage(validationMessagesKeys.noAccess, this.props.t),
                        this.state.dotfilesRepository.value
                    );
                }
                if (
                    validationMessage !== validationMessagesKeys.valid ||
                    dotfilesValidationMessage !== validationMessagesKeys.valid
                ) {
                    throw new Error('Failed to access git repositories.');
                }

                this.setState({ isCreatingEnvironment: true });
                await this.props.onCreateEnvironment(this.getEnvCreationParams());
                this.handleErrorMessage(true);
                return;
            } catch (err) {
                if (this.state.authenticationAttempt) {
                    this.state.authenticationAttempt.dispose();
                }
                this.setState({
                    shouldTryToAuthenticateForRepo: false,
                    shouldTryToAuthenticateForDotfiles: false,
                    authenticationAttempt: undefined,
                    isCreatingEnvironment: false,
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

        this.setState({ isCreatingEnvironment: true });
        await this.props.onCreateEnvironment(this.getEnvCreationParams());
        this.handleErrorMessage(false);
    };

    private async getGitHubAndAzureDevOpsAccessTokens() {
        let gitServiceProviders = [];
        if (this.state.shouldTryToAuthenticateForRepo) {
            const normalizedGitUrl = normalizeGitUrl(this.state.gitRepositoryUrl.value);
            if (normalizedGitUrl) {
                gitServiceProviders.push(getSupportedGitService(normalizedGitUrl));
            }
        }
        if (this.state.shouldTryToAuthenticateForDotfiles) {
            const normalizedGitUrl = normalizeGitUrl(this.state.dotfilesRepository.value);
            if (normalizedGitUrl) {
                let dotFilesGitServiceProvider = getSupportedGitService(normalizedGitUrl);
                if (
                    gitServiceProviders.length === 0 ||
                    gitServiceProviders[0] !== dotFilesGitServiceProvider
                ) {
                    gitServiceProviders.push(dotFilesGitServiceProvider);
                }
            }
        }
        for (let gitServiceProvider of gitServiceProviders) {
            let authenticationAttempt: IAuthenticationAttempt;
            if (this.state.authenticationAttempt) {
                authenticationAttempt = this.state.authenticationAttempt;
            } else {
                switch (gitServiceProvider) {
                    case SupportedGitService.AzureDevOps:
                        authenticationAttempt = new AzDevAuthenticationAttempt();
                        break;
                    default:
                        authenticationAttempt = new GithubAuthenticationAttempt();
                }
                this.setState({
                    authenticationAttempt,
                });
            }
            let accessToken;
            switch (gitServiceProvider) {
                case SupportedGitService.AzureDevOps:
                    accessToken = await getAzDevCredentials(authenticationAttempt);
                    break;
                default:
                    accessToken = await getGitHubCredentials(authenticationAttempt);
            }
            if (this.state.authenticationAttempt) {
                this.state.authenticationAttempt.dispose();
            }
            this.setState({
                authenticationAttempt: undefined,
            });
            if (!accessToken) {
                switch (gitServiceProvider) {
                    case SupportedGitService.AzureDevOps:
                        throw new Error('Failed to authenticate against Azure DevOps.');
                    default:
                        throw new Error('Failed to authenticate against GitHub.');
                }
            }
            switch (gitServiceProvider) {
                case SupportedGitService.AzureDevOps:
                    this.props.storeAzDevCredentials(accessToken);
                    break;
                default:
                    this.props.storeGitHubCredentials(accessToken);
            }
        }
    }

    private handleErrorMessage(handleAuth: boolean) {
        if (this.props.errorMessage) {
            this.setState({
                isCreatingEnvironment: false,
                friendlyName: {
                    ...this.state.friendlyName,
                    errorMessage: this.props.errorMessage,
                    style: errorTextfieldClassname,
                },
            });
            this.onNotifyValidationResultFriendlyName(
                this.props.errorMessage,
                this.state.friendlyName.value
            );
            if (handleAuth) {
                this.setState({
                    shouldTryToAuthenticateForRepo: false,
                    shouldTryToAuthenticateForDotfiles: false,
                    authenticationAttempt: undefined,
                });
            }
        }
    }

    private getEnvCreationParams() {
        const { selectedPlan } = this.props;

        if (!selectedPlan) {
            throw new Error('No plan selected.');
        }

        const envParams = formToEnvironmentParams(selectedPlan, this.state);
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
        this.setState({
            friendlyName: {
                ...this.state.friendlyName,
                errorMessage: undefined,
                style: undefined,
            },
        });
        this.setTextValidationState('friendlyName', value, ValidationState.Validating);
    };

    private onGetErrorMessageFriendlyName = (value: string) => {
        value = value.trim();

        // Regex pattern for naming, can include alphanumeric, space, underscore, parentheses, hyphen, period, and Unicode characters that match the allowed characters.
        const regex = /^[-\w\._\(\) ]+$/g;

        if (value.length === 0) {
            return getValidationMessage(validationMessagesKeys.nameIsRequired, this.props.t);
        } else if (value.length > 90) {
            return getValidationMessage(validationMessagesKeys.nameIsTooLong, this.props.t);
        } else if (!regex.test(value)) {
            return getValidationMessage(validationMessagesKeys.nameIsInvalid, this.props.t);
        }
    };

    private onNotifyValidationResultFriendlyName = (
        errorMessage: string | ReactElement,
        value = ''
    ) => {
        const error = errorMessage || this.state.friendlyName.errorMessage;
        const validationState = error ? ValidationState.Invalid : ValidationState.Valid;
        this.setTextValidationState('friendlyName', value, validationState);
    };

    private onChangeGitRepositoryUrl = (_event: unknown, value = '') => {
        this.setState({
            shouldTryToAuthenticateForRepo: false,
            gitRepositoryUrl: {
                ...this.state.gitRepositoryUrl,
                errorMessage: undefined,
                style: undefined,
            },
        });

        this.setTextValidationState('gitRepositoryUrl', value, ValidationState.Validating);
    };

    private showRepoError() {
        this.setState({
            gitRepositoryUrl: {
                ...this.state.gitRepositoryUrl,
                errorMessage: getValidationMessage(validationMessagesKeys.noAccess, this.props.t),
                style: errorTextfieldClassname,
            },
        });
    }

    private showDotfilesError() {
        this.setState({
            dotfilesRepository: {
                ...this.state.dotfilesRepository,
                errorMessage: getValidationMessage(validationMessagesKeys.noAccessDotFiles, this.props.t),
                style: errorTextfieldClassname,
            },
        });
    }

    private onGetErrorMessageGitRepo = async (value: string) => {
        return await this.onGetErrorMessage(value, 'git');
    };

    private onGetErrorMessageDotfilesRepo = async (value: string) => {
        return await this.onGetErrorMessage(value, 'dotfiles');
    };

    private onGetErrorMessage = async (value: string, textField: string) => {
        const validationResult = await validateGitRepository(
            value,
            this.props.gitHubAccessToken,
            this.props.azDevAccessToken
        );
        switch (validationResult) {
            case validationMessagesKeys.invalidGitUrl:
            case validationMessagesKeys.unableToConnect:
            case validationMessagesKeys.testFailed: {
                return getValidationMessage(validationResult, this.props.t);
            }

            case validationMessagesKeys.noAccess: {
                if (textField === 'git') {
                    this.showRepoError();
                } else if (textField === 'dotfiles') {
                    this.showDotfilesError();
                }
                return getValidationMessage(validationMessagesKeys.valid, this.props.t);
            }

            case validationMessagesKeys.privateRepoNoAuth: {
                if (textField === 'git') {
                    this.setState({
                        shouldTryToAuthenticateForRepo: true,
                    });
                } else if (textField === 'dotfiles') {
                    this.setState({
                        shouldTryToAuthenticateForDotfiles: true,
                    });
                }
                return getValidationMessage(validationMessagesKeys.valid, this.props.t);
            }

            default: {
                return getValidationMessage(validationMessagesKeys.valid, this.props.t);
            }
        }
    };

    private getSkuNameValidationMessage() {
        if (this.props.isPlanLoadingFinished) {
            if (!this.props.selectedPlan) {
                return getValidationMessage(validationMessagesKeys.noPlanSelected, this.props.t);
            } else if (!isNotNullOrEmpty(this.props.selectedPlan.availableSkus)) {
                return getValidationMessage(validationMessagesKeys.noSkusAvailable, this.props.t);
            }
        }
    }

    private onNotifyValidationResultGitRepositoryUrl = (
        errorMessage: string | ReactElement,
        value = ''
    ) => {
        const error = errorMessage || this.state.gitRepositoryUrl.errorMessage;
        const validationState = error ? ValidationState.Invalid : ValidationState.Valid;
        this.setTextValidationState('gitRepositoryUrl', value, validationState);
    };

    private onChangeDotfilesRepository = (_event: unknown, value = '') => {
        this.setState({
            shouldTryToAuthenticateForDotfiles: false,
            dotfilesRepository: {
                ...this.state.dotfilesRepository,
                errorMessage: undefined,
                style: undefined,
            },
        });

        this.setTextValidationState('dotfilesRepository', value, ValidationState.Validating);
    };

    private onNotifyValidationResultDotfilesRepository = (
        errorMessage: string | ReactElement,
        value = ''
    ) => {
        const error = errorMessage || this.state.dotfilesRepository.errorMessage;
        const validationState = error ? ValidationState.Invalid : ValidationState.Valid;
        this.setTextValidationState('dotfilesRepository', value, validationState);
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

    private onChangeSkuName = (_event: unknown, option?: IDropdownOption, index?: number) => {
        if (!option) {
            return;
        }

        if (typeof option.key !== 'string') {
            throw new Error('NotImplemented');
        }

        if (option.key === SKU_SHOW_PRICING_KEY) {
            return;
        }

        const newState = this.isSkuNameValid(option.key, this.getAvailableSkus())
            ? ValidationState.Valid
            : ValidationState.Invalid;

        this.setTextValidationState('skuName', option.key, newState);
    };

    private isSkuNameValid = (skuName: string, availableSkus?: ISku[] | null) => {
        return availableSkus && availableSkus!.filter((s) => s.name === skuName).length > 0;
    };
}

export const CreateEnvironmentPanel = withTranslation()(connect(
    ({
        githubAuthentication: { gitHubAccessToken },
        azDevAuthentication: { azDevAccessToken },
        plans: { selectedPlan, isLoadingPlan, isMadeInitialPlansRequest },
    }: ApplicationState) => {
        return {
            gitHubAccessToken,
            azDevAccessToken,
            selectedPlan,
            isPlanLoadingFinished: isMadeInitialPlansRequest && !isLoadingPlan,
        };
    },
    {
        storeGitHubCredentials,
        storeAzDevCredentials,
    }
)(CreateEnvironmentPanelView));
