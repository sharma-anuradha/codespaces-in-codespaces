import React, { Component, FormEvent } from 'react';
import { connect } from 'react-redux';
import { PrimaryButton, DefaultButton, IconButton } from 'office-ui-fabric-react/lib/Button';
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { IDropdown, IDropdownOption, IDropdownProps } from 'office-ui-fabric-react/lib/Dropdown';
import { ComboBox, IComboBoxOption, IComboBoxProps } from 'office-ui-fabric-react/lib/ComboBox';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { KeyCodes, IRenderFunction } from '@uifabric/utilities';

import { isDefined } from 'vso-client-core';

import { DropDownWithLoader } from '../dropdown-with-loader/dropdown-with-loader';
import { ISelectableOption } from 'office-ui-fabric-react/lib/utilities/selectableOption/SelectableOption.types';

import { Loader } from '../loader/loader';

import { authService } from '../../services/authService';
import { armAPIVersion } from '../../constants';
import { getPlans } from '../../actions/plans-actions';
import { ApplicationState } from '../../reducers/rootReducer';
import { ConfigurationState } from '../../reducers/configuration';
import { IToken } from '../../typings/IToken';
import { wait } from '../../dependencies';

import { IAzureSubscription } from '../../interfaces/IAzureSubscription';
import { Collapsible } from '../collapsible/collapsible';
import { createUniqueId } from '../../dependencies';
import { isNotNullOrEmpty } from '../../utils/isNotNullOrEmpty';
import { PlanCreationError, PlanCreationFailureReason } from './PlanCreationError';
import { createPlan } from '../../actions/createPlan';
import { createResourceGroup } from '../../actions/createResourceGroup';
import { locationToDisplayName } from '../../utils/locations';
import { ILocations } from '../../interfaces/ILocation';
import { getLocation } from '../../actions/locations-actions';
import { getSkuSpecLabel } from '../../utils/environmentUtils';
import { WithTranslation, withTranslation } from 'react-i18next';

const SKU_SHOW_PRICING_KEY = 'show-pricing';
const SKU_PRICING_LABEL = 'Show pricing information...';
const SKU_PRICING_URL = 'https://aka.ms/vso-pricing';

const RESOURCE_REGISTRATION_POLLING_INTERVAL_MS = 300;
const RESOURCE_REGISTRATION_MAX_POLLS = 100;

export interface CreatePlanPanelProps extends WithTranslation {
    hidePanel: (canContinueToEnvironment?: boolean) => void;
    createPlan: typeof createPlan;
    createResourceGroup: typeof createResourceGroup;
    configuration: ConfigurationState;
    locations: ILocations;
}

interface IStringOption {
    key: string;
    text: string;
}

interface INewGroupOption extends IStringOption {
    isNewGroup?: boolean;
}

const asc = (a: IStringOption, b: IStringOption) => (a.text > b.text ? 1 : -1);
const noSkuSelectedKey:string = '';
const noSkuSelectedText:string = '--';

const subscriptionIdStorageKey = 'user_setting_planSubscription';

export interface CreatePlanPanelState {
    userSpecifiedPlanName?: string;
    subscriptionList: IStringOption[];
    resourceGroupList: INewGroupOption[];
    locationsList: IStringOption[];
    skuList: IDropdownOption[];
    selectedSubscription?: string;
    newGroup?: string;
    userSelectedResourceGroup?: string;
    selectedLocation?: string;
    isCreatingPlan: boolean;
    isGettingSubscriptions: boolean;
    isGettingResourceGroups: boolean;
    isGettingClosestLocation: boolean;
    isGettingSkuList: boolean;
    selectedDefaultSku?: string,
    errorMessage?: string;
}

function nameHasInvalidEnding(name: string) {
    // Naming guidelines suggest names should not start or end with period or hyphen.
    return /^[-\.]/.test(name) || /[-\.]$/.test(name);
}

function validateResourceName(name: string): string | undefined {
    if (name.length > 60) {
        return 'Name is too long.';
    } else if (
        !/^[-\w\._]+$/.test(name) || // Contains invalid characters
        nameHasInvalidEnding(name)
    ) {
        return 'Name contains invalid characters.';
    }
    return undefined;
}

export class CreatePlanPanelComponent extends Component<
    CreatePlanPanelProps,
    CreatePlanPanelState
> {
    public constructor(props: CreatePlanPanelProps) {
        super(props);

        this.state = {
            subscriptionList: [],
            newGroup: `vscs-rg-${createUniqueId().substr(0, 7)}`,
            resourceGroupList: [],
            locationsList: [],
            skuList: [],
            isCreatingPlan: false,
            isGettingSubscriptions: true,
            isGettingResourceGroups: true,
            isGettingClosestLocation: true,
            isGettingSkuList: true,
        };
    }

    componentDidMount() {
        this.getSubscriptions();
        this.getClosestLocation();
        document.addEventListener('keydown', this.dismissPanel);
    }

    componentWillUnmount() {
        document.removeEventListener('keydown', this.dismissPanel);
    }

    render() {
        const {
            isGettingSubscriptions,
            isGettingClosestLocation,
            isGettingSkuList,
            subscriptionList,
            resourceGroupList,
            locationsList,
            selectedSubscription,
            selectedLocation,
            newGroup,
            selectedDefaultSku,
        } = this.state;

        const resourceGroupDescription = isDefined(newGroup)
            ? 'We will create a new resource group for you automatically.'
            : null;

        const isSubscriptionEmpty =
            !this.state.isGettingSubscriptions &&
            this.state.subscriptionList &&
            this.state.subscriptionList.length === 0;
        const { t: translation } = this.props;

        return (
            <Panel
                isOpen={true}
                type={PanelType.smallFixedFar}
                headerText='Create a Billing Plan'
                closeButtonAriaLabel='Close'
                onDismiss={this.onDismissPanel}
                isFooterAtBottom={true}
                onRenderFooterContent={this.onRenderFooterContent}
                className='create-environment-panel'
            >
                <Stack tokens={{ childrenGap: 'l1' }}>
                    {this.renderOverlay()}

                    <Stack tokens={{ childrenGap: 4 }}>
                        <DropDownWithLoader
                            label='Subscription'
                            ariaLabel='Subscription'
                            onChange={this.subscriptionChanged}
                            options={subscriptionList}
                            isLoading={isGettingSubscriptions}
                            disabled={isSubscriptionEmpty}
                            loadingMessage='Fetching your subscriptions...'
                            selectedKey={selectedSubscription}
                            className='create-environment-panel__dropdown'
                            translation={translation}
                        />
                        {isSubscriptionEmpty && (
                            <Link href='https://azure.microsoft.com/en-us/free/' target='_blank'>
                                Create a free Azure account
                            </Link>
                        )}
                        <DropDownWithLoader
                            label='Location'
                            ariaLabel='Location'
                            onChange={this.locationChanged}
                            options={locationsList}
                            loadingMessage='Determining the closest location...'
                            isLoading={isGettingClosestLocation}
                            selectedKey={selectedLocation}
                            className='create-environment-panel__dropdown'
                            translation={translation}
                        />
                    </Stack>

                    <Collapsible tokens={{ childrenGap: 4 }} title='Advanced Options'>
                        <TextField
                            label='Plan Name'
                            required
                            placeholder='My plan'
                            onChange={this.planNameChanged}
                            onGetErrorMessage={validateResourceName}
                            value={this.planName}
                        />
                        <ComboBox
                            label='Resource Group'
                            onChange={this.resourceGroupChanged}
                            options={resourceGroupList}
                            onResolveOptions={this.resolveResourceGroups}
                            disabled={!selectedSubscription}
                            allowFreeform
                            autoComplete='on'
                            useComboBoxAsMenuWidth={true}
                            selectedKey={this.selectedResourceGroup}
                            text={newGroup}
                            errorMessage={this.getResourceGroupValidation()}
                            className='create-environment-panel__dropdown'
                        />

                        {resourceGroupDescription}

                        {this.renderSkuSelector()}

                    </Collapsible>
                </Stack>
            </Panel>
        );
    }

    getResourceGroupValidation(): string | undefined {
        const { newGroup } = this.state;
        const { t: translation } = this.props;

        if (newGroup && this.selectedResourceGroup && newGroup === this.selectedResourceGroup) {
            return validateResourceName(newGroup);
        }
        return undefined;
    }

    private renderOverlay() {
        const { isCreatingPlan } = this.state;
        const { t: translation } = this.props;

        if (!isCreatingPlan || !this.planName) {
            return null;
        }

        return (
            <div className='create-environment-panel__overlay'>
                <Loader message={translation('creatingPlan')} translation={translation} />
            </div>
        );
    }

    private skuDropdownRef = React.createRef<IDropdown>();

    private renderSkuSelector() {
        const { t: translation } = this.props;
        const onSkuLabelRender = createLabelRenderCallback(SKU_PRICING_LABEL, SKU_PRICING_URL);
        return (
            <DropDownWithLoader
                componentRef={this.skuDropdownRef}
                label='Default Instance Type'
                ariaLabel='Default Instance Type'
                options={this.state.skuList}
                isLoading={this.state.isGettingSkuList}
                loadingMessage='Loading available instance types'
                selectedKey={this.state.selectedDefaultSku}
                onChange={this.defaultSkuChanged}
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

    private hideErrorMessage = () => {
        this.setState({
            errorMessage: undefined,
        });
    };

    private onRenderFooterContent = () => {
        const errorMessage = this.state.errorMessage ? (
            <MessageBar
                messageBarType={MessageBarType.error}
                isMultiline={true}
                onDismiss={this.hideErrorMessage}
                dismissButtonAriaLabel={'Close'}
            >
                {this.state.errorMessage}
            </MessageBar>
        ) : null;

        return (
            <Stack tokens={{ childrenGap: 'l1' }}>
                <Stack.Item>{errorMessage}</Stack.Item>

                <Stack.Item>
                    <PrimaryButton
                        onClick={this.createPlan}
                        style={{ marginRight: '.8rem' }}
                        disabled={!this.isCurrentStateValid()}
                    >
                        Create
                    </PrimaryButton>
                    <DefaultButton onClick={this.clearForm}>Cancel</DefaultButton>
                </Stack.Item>
            </Stack>
        );
    };

    dismissPanel = (event: KeyboardEvent) => {
        if (event.keyCode === KeyCodes.escape) {
            this.clearForm();
        }
    };

    private onDismissPanel = () => {
        this.props.hidePanel();
    };

    private createPlan = async () => {
        if (!this.isCurrentStateValid()) {
            return;
        }
        const { t: translation } = this.props;

        try {
            this.setState({ isCreatingPlan: true });

            if (!this.state.selectedSubscription) {
                throw new PlanCreationError(PlanCreationFailureReason.NoSubscription);
            }
            if (!this.selectedResourceGroup) {
                throw new PlanCreationError(PlanCreationFailureReason.NoResourceGroup);
            }
            if (!this.state.selectedLocation) {
                throw new PlanCreationError(PlanCreationFailureReason.NoLocation);
            }

            let selectedDefaultSku = this.state.selectedDefaultSku;
            if (selectedDefaultSku == noSkuSelectedKey) {
                selectedDefaultSku = undefined;
            }

            const subscriptionId = this.state.selectedSubscription;
            const resourceGroupName = this.selectedResourceGroup;
            const location = this.state.selectedLocation;

            let resourceGroupPath = resourceGroupName;
            if (resourceGroupName === this.state.newGroup) {
                resourceGroupPath = await this.props.createResourceGroup(
                    subscriptionId,
                    resourceGroupName,
                    location
                );
            }

            await this.checkResourceProvider(subscriptionId);

            await this.props.createPlan(resourceGroupPath, this.planName, location, selectedDefaultSku);

            await getPlans();
            this.props.hidePanel(true);
        } catch (e) {
            if (e instanceof PlanCreationError) {
                this.setState({
                    errorMessage: e.message,
                });
            }

            this.setState({ isCreatingPlan: false });
        }
    };

    private clearForm = () => {
        this.setState({ userSpecifiedPlanName: undefined, errorMessage: undefined });
        this.props.hidePanel();
    };

    private async registerResourceProvider(subscription: string) {
        const registerUrl = `https://management.azure.com/subscriptions/${subscription}/providers/Microsoft.VSOnline/register?api-version=2019-08-01`;
        let resp = await fetch(registerUrl, {
            method: 'POST',
            headers: {
                authorization: `Bearer ${(await this.getAuthToken()).accessToken}`,
            },
        });
        if (resp.status != 200) {
            throw new PlanCreationError(PlanCreationFailureReason.FailedToRegisterResourceProvider);
        }

        let retries = RESOURCE_REGISTRATION_MAX_POLLS;
        while (--retries && !await this.isResourceProviderRegistered(subscription)) {
            await wait(RESOURCE_REGISTRATION_POLLING_INTERVAL_MS);
        }
        if (!retries) {
            throw new PlanCreationError(PlanCreationFailureReason.TimeoutWaitingForRegisterResourceProvider);
        }
    }

    private async isResourceProviderRegistered(subscription: string) {
        const url = `https://management.azure.com/subscriptions/${subscription}/providers/Microsoft.VSOnline?api-version=2019-08-01`;
        let response = await this.getFromAzure(url);
        return response && response.registrationState == 'Registered';
    }

    private async getAuthToken(): Promise<IToken> {
        const myAuthToken = await authService.getARMToken(60);
        if (!myAuthToken) {
            throw new PlanCreationError(PlanCreationFailureReason.NotAuthenticated);
        }
        return myAuthToken
    }

    private async checkResourceProvider(subscription: string) {
        if (!await this.isResourceProviderRegistered(subscription)) {
            await this.registerResourceProvider(subscription);
        }
    }

    private async getFromAzure(url: string) {
        const myAuthToken = await this.getAuthToken();

        const authToken = 'Bearer ' + myAuthToken.accessToken;
        let response = await fetch(url, {
            headers: {
                authorization: authToken,
            },
        });
        if (!response) {
            throw new Error(`Azure GET request failed`);
        }
        if (response.status === 200) {
            return response.json();
        } else {
            // tslint:disable-next-line:no-console
            console.error(`Request to ${response.url} failed with status ${response.status}`);
            return null;
        }
    }

    private async getSubscriptions() {
        try {
            this.setState({
                isGettingSubscriptions: true,
            });

            const myJson = await this.getFromAzure(
                `https://management.azure.com/subscriptions?api-version=${armAPIVersion}`
            );

            if (myJson) {
                const subscriptionList: IStringOption[] = myJson.value.map(
                    (sub: IAzureSubscription) => {
                        return {
                            key: sub.subscriptionId,
                            text: sub.displayName,
                            title: `Subscription Id: ${sub.subscriptionId}`,
                        };
                    }
                );

                this.setState({
                    subscriptionList: subscriptionList.sort(asc),
                });

                // if no subscription selected, select the first one by default
                const maybeSubscriptionId = localStorage.getItem(subscriptionIdStorageKey);
                const defaultSubscription =
                    subscriptionList.find((sub) => sub.key === maybeSubscriptionId) ||
                    subscriptionList[0];
                if (defaultSubscription && !this.state.selectedSubscription) {
                    this.subscriptionChanged({}, defaultSubscription);
                }
            }
        } finally {
            this.setState({ isGettingSubscriptions: false });
        }
    }

    private async getClosestLocation() {
        try {
            const { configuration, locations } = this.props;

            if (!configuration) {
                return;
            }

            this.setState({ isGettingClosestLocation: true });

            const closestLocation: string = locations.current;

            const { selectedLocation } = this.state;

            const locationsList = locations.available.map((l: string) => {
                return {
                    key: l,
                    text: locationToDisplayName(l),
                };
            });

            await this.getAvailableSkus(selectedLocation || closestLocation);

            this.setState({
                selectedLocation: selectedLocation || closestLocation,
                locationsList: locationsList.sort(asc),
            });
        } catch {
            // ignore
        } finally {
            this.setState({ isGettingClosestLocation: false });
        }
    }

    private async getAvailableSkus(location: string) {

        try {

            const originalSelectedDefaultSku = this.state.selectedDefaultSku;
            this.setState({ isGettingSkuList: true, skuList: [], selectedDefaultSku: '' });

            const locationInfo = await getLocation(location, undefined);
            const skus = locationInfo?.skus;
            const { t: translation } = this.props;

            let skuListOptions: IDropdownOption[] = []

            if (skus && skus.length > 0) {
                skuListOptions = locationInfo.skus.map((s) => {
                    const text = getSkuSpecLabel(s, translation);
                    return { key: s.name, text };
                });
            }

            skuListOptions.push({
                key: SKU_SHOW_PRICING_KEY,
                text: SKU_PRICING_LABEL,
                data: { icon: 'OpenInNewTab', url: SKU_PRICING_URL },
                ariaLabel: `${SKU_PRICING_LABEL}, ${SKU_PRICING_URL}`,
            });

            skuListOptions.unshift({
                key: noSkuSelectedKey,
                text: noSkuSelectedText,
            })

            this.setState({
                skuList: skuListOptions,
            });

            if (originalSelectedDefaultSku && originalSelectedDefaultSku != noSkuSelectedKey) {
                if (skus && skus.find(s => s.name == originalSelectedDefaultSku)) {
                    this.setState({
                        selectedDefaultSku: originalSelectedDefaultSku,
                    });
                }
            }
        } catch {
            // ignore
        } finally {
            this.setState({ isGettingSkuList: false });
        }
    }

    private resolveResourceGroups = async (): Promise<IComboBoxOption[]> => {
        if (!this.state.selectedSubscription) {
            throw new Error('NoSubscription');
        }

        if (isNotNullOrEmpty(this.state.resourceGroupList)) {
            return this.state.resourceGroupList;
        }

        const url = new URL(
            `/subscriptions/${this.state.selectedSubscription}/resourcegroups`,
            'https://management.azure.com'
        );
        url.searchParams.set('api-version', armAPIVersion);

        let resourceGroupList: INewGroupOption[] = [];
        const myJson = await this.getFromAzure(url.toString());

        if (myJson) {
            if (this.state.newGroup) {
                resourceGroupList.push({
                    key: this.state.newGroup,
                    text: this.state.newGroup,
                    isNewGroup: true,
                });
            }

            for (let resGroup of myJson.value) {
                resourceGroupList.push({ key: resGroup.id, text: resGroup.name });
            }

            this.setState({
                resourceGroupList: resourceGroupList.sort(asc),
            });
        }

        return resourceGroupList;
    };

    private isCurrentStateValid() {
        let isInvalid = false;

        if (!this.state.selectedLocation || !this.state.selectedSubscription) {
            isInvalid = true;
        }

        if (this.state.newGroup && this.selectedResourceGroup === this.state.newGroup) {
            isInvalid = isInvalid || isNotNullOrEmpty(validateResourceName(this.state.newGroup));
        }

        isInvalid = isInvalid || !this.selectedResourceGroup;

        const planName = this.planName.trim();
        isInvalid = isInvalid || !planName || isNotNullOrEmpty(validateResourceName(planName));

        return !isInvalid;
    }

    // Functions that handle dropdown change events

    private planNameChanged: (
        event: FormEvent<HTMLInputElement | HTMLTextAreaElement>,
        planName?: string
    ) => void = (_event, planName) => {
        this.setState({
            userSpecifiedPlanName: planName,
            errorMessage: undefined,
        });
    };

    private get automaticallyGeneratedPlanName() {
        if (!this.state.selectedLocation) {
            return '';
        }

        return `vscs-plan-${this.state.selectedLocation.toLowerCase()}`;
    }

    private get planName() {
        return isDefined(this.state.userSpecifiedPlanName)
            ? this.state.userSpecifiedPlanName
            : this.automaticallyGeneratedPlanName;
    }

    private subscriptionChanged: (_: unknown, option?: IDropdownOption, index?: number) => void = (
        _e,
        option
    ) => {
        if (!option) {
            return;
        }

        if (typeof option.key !== 'string') {
            throw new Error('Subscription id should be a string');
        }

        this.setState({
            selectedSubscription: option.key,
            resourceGroupList: [],
            errorMessage: undefined,
        });

        if (this.state.selectedSubscription) {
            localStorage.setItem(subscriptionIdStorageKey, option.key);
        }

        return;
    };

    private resourceGroupChanged: IComboBoxProps['onChange'] = (
        _event,
        option,
        _index,
        value = ''
    ) => {
        const updateNewResourceGroupItem = (groupName: string) => {
            const { resourceGroupList } = this.state;
            if (!isNotNullOrEmpty(this.state.resourceGroupList)) {
                return;
            }

            this.setState({
                resourceGroupList: [
                    { key: groupName, text: groupName, isNewGroup: true },
                    ...resourceGroupList.filter((g) => !g.isNewGroup),
                ].sort(asc),
            });
        };

        if (option) {
            if (typeof option.key === 'number') {
                throw new Error('Resource group ids should always be a strings.');
            }

            // We keep a flag on the the new group option so it's re-selectable
            const isNewGroup = !!(option as INewGroupOption).isNewGroup;
            if (isNewGroup) {
                this.setState({
                    newGroup: option.key,
                    userSelectedResourceGroup: undefined,
                    errorMessage: undefined,
                });
            } else {
                this.setState({
                    newGroup: undefined,
                    userSelectedResourceGroup: option.key,
                    errorMessage: undefined,
                });
            }
        } else if (value) {
            this.setState({
                newGroup: value,
                userSelectedResourceGroup: undefined,
                errorMessage: undefined,
            });

            updateNewResourceGroupItem(value);
        }
    };

    private get selectedResourceGroup() {
        return this.state.userSelectedResourceGroup || this.state.newGroup;
    }

    private locationChanged: (
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option) => {
        if (!option) {
            return;
        }

        if (typeof option.key === 'number') {
            throw new Error('Resource group id should always be a string.');
        }

        this.getAvailableSkus(option.key);

        this.setState({
            selectedLocation: option.key,
            errorMessage: undefined,
        });
    };

    private defaultSkuChanged: (
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option) => {
        if (!option) {
            return;
        }

        if (option.key === SKU_SHOW_PRICING_KEY) {
            return;
        }

        if (typeof option.key === 'number') {
            throw new Error('Default Sku name should always be a string.');
        }

        this.setState({
            selectedDefaultSku: option.key,
            errorMessage: undefined,
        });
    };
}

const openExternalUrl = (url: string) => {
    window.open(url, '_blank');
};
function createLabelRenderCallback(title: string, onClickUrl: string, customLabel?: string) {
    return (props: IDropdownProps) => {
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
export const CreatePlanPanel = withTranslation()(connect(    ({ configuration, locations }: ApplicationState) => ({
        configuration,
        locations
    }),
    {
        createResourceGroup,
        createPlan,
    }
)(CreatePlanPanelComponent));
