import React, { useState, useCallback, useEffect } from 'react';
import moment from 'moment';

import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { Text } from 'office-ui-fabric-react/lib/Text';
import { getTheme } from 'office-ui-fabric-react/lib/Styling';
import { IconButton, PrimaryButton, DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Dialog, DialogType, DialogFooter } from 'office-ui-fabric-react/lib/Dialog';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { ProgressIndicator } from 'office-ui-fabric-react/lib/ProgressIndicator';
import { SharedColors, NeutralColors } from '@uifabric/fluent-theme/lib/fluent/FluentColors';

import { deleteEnvironment } from '../../actions/deleteEnvironment';
import { shutdownEnvironment } from '../../actions/shutdownEnvironment';
import {
    environmentIsALie,
    isNotConnectable,
    isNotSuspendable,
    isSelfHostedEnvironment,
    isActivating,
    isInStableState,
    getSkuSpecLabel,
    stateToDisplayName,
    environmentErrorCodeToString,
} from '../../utils/environmentUtils';
import { tryOpeningUrl } from '../../utils/vscodeProtocolUtil';
import './environment-card.css';
import { useSelector } from 'react-redux';
import { ApplicationState } from '../../reducers/rootReducer';
import { ActivePlanInfo } from '../../reducers/plans-reducer';
import { MessageBar, MessageBarType, IDropdownOption } from 'office-ui-fabric-react';
import { DropDownWithLoader } from '../dropdown-with-loader/dropdown-with-loader';
import {
    EnvironmentSettingsAllowedUpdates,
    EnvironmentSettingsUpdate,
} from '../../interfaces/cloudenvironment';
import {
    getAllowedEnvironmentSettingsChanges,
    updateEnvironmentSettings,
} from '../../actions/environmentSettingsChanges';
import { Loader } from '../loader/loader';
import { ServiceResponseError } from '../../actions/middleware/useWebClient';
import {
    Signal,
    ILocalEnvironment,
    EnvironmentStateInfo,
    isDefined,
    EnvironmentErrorCodes,
    IEnvironment,
} from 'vso-client-core';
import { useTranslation } from 'react-i18next';
import { injectMessageParameters } from '../../utils/injectMessageParameters';
import { TFunction } from 'i18next';

const friendlyNameDisplayLength = 20;
export interface EnvironmentCardProps {
    className?: string;
    environment: ILocalEnvironment;
    deleteEnvironment: (...params: Parameters<typeof deleteEnvironment>) => void;
    shutdownEnvironment: (...params: Parameters<typeof shutdownEnvironment>) => void;
}

function ThinSeparator() {
    return (
        <div
            style={{ backgroundColor: NeutralColors.gray20 }}
            className='environment-card__thin-separator'
        />
    );
}

enum EnvironmentSettingsChangeState {
    // Normal text display
    Display,
    // Enabled dropdown
    Editing,
    // Disabled dropdown
    Submitting,
}

type DetailsProps = {
    details: Detail[];
    settingsChangeState: EnvironmentSettingsChangeState;
};

type Detail = {
    // The name of the setting
    key: string;
    // The value of the setting (or the new value if editing)
    value: any;
    // The display text for the setting
    text: string;
    // If settingsChangeState !== display and this are true, will show a dropdown
    editable: boolean;
    // Options for the dropdown while editing
    editOptions?: IDropdownOption[];
    // Callback for the edit dropdown selection
    setValue?: (option: IDropdownOption | undefined) => void;
};

function Details({ details, settingsChangeState }: DetailsProps) {
    const rows = details.map((d) => (
        <div className='environment-card__details-table-row' key={`${d.key}${d.text}`}>
            <div className='environment-card__details-table-cell environment-card__details-label'>
                <Text
                    nowrap
                    block
                    className={'environment-card__details-label-text'}
                    style={{ color: NeutralColors.gray160 }}
                >
                    {d.key}
                </Text>
            </div>
            <div className='environment-card__details-table-cell environment-card__details-value'>
                <DetailValueOrEdit detail={d} settingsChangeState={settingsChangeState} />
            </div>
        </div>
    ));

    return <div className='environment-card__details-table'>{rows}</div>;
}

function DetailValueOrEdit({
    detail,
    settingsChangeState,
}: {
    detail: Detail;
    settingsChangeState: EnvironmentSettingsChangeState;
}) {
    const { value, text, editable, editOptions, setValue } = detail;
    const { t: translation } = useTranslation();

    if (settingsChangeState === EnvironmentSettingsChangeState.Display || !editable) {
        return (
            <Text nowrap block style={{ color: NeutralColors.black }}>
                {text}
            </Text>
        );
    } else {
        const currentValueOption = editOptions && editOptions.find((o) => o.key === value);

        const disabled =
            settingsChangeState !== EnvironmentSettingsChangeState.Editing ||
            (editOptions && editOptions.length < 2);

        return (
            <DropDownWithLoader
                className={'environment-card__details-edit-dropdown'}
                isLoading={!editOptions}
                options={editOptions || []}
                selectedKey={currentValueOption && currentValueOption.key}
                loadingMessage={translation('loadingAvailableSettings')}
                disabled={disabled}
                // tslint:disable-next-line: react-this-binding-issue
                onChange={(_, option) => setValue!(option)}
                translation={translation}
            />
        );
    }
}

function Status({ environment }: { environment: ILocalEnvironment }) {
    const { t: translation } = useTranslation();
    let backgroundColor;
    let color;
    switch (environment.state) {
        case EnvironmentStateInfo.Available:
            backgroundColor = '#6BB700';
            color = NeutralColors.gray160;
            break;
        case EnvironmentStateInfo.Deleted:
        case EnvironmentStateInfo.Failed:
            backgroundColor = SharedColors.red20;
            color = NeutralColors.gray10;
            break;
        default:
            backgroundColor = NeutralColors.gray20;
            color = NeutralColors.gray160;
    }

    return (
        <Text
            title={environmentErrorCodeToString(Number(environment.lastStateUpdateReason), translation)}
            block
            className='environment-card__status'
            style={{
                color,
                backgroundColor,
            }}
        >
            {stateToDisplayName(environment.state, translation)}
        </Text>
    );
}

type ActionProps = {
    environment: ILocalEnvironment;
    deleteEnvironment: (...params: Parameters<typeof deleteEnvironment>) => void;
    shutdownEnvironment: (...params: Parameters<typeof shutdownEnvironment>) => void;
    enableEditing: () => void;
};

// tslint:disable-next-line: max-func-body-length
const Actions = ({
    environment,
    deleteEnvironment,
    shutdownEnvironment,
    enableEditing,
}: ActionProps) => {
    const [deleteDialogHidden, setDeleteDialogHidden] = useState(true);
    const [unsuccessfulUrlDialogHidden, setUnsuccessfulUrlDialogHidden] = useState(true);
    const [shutdownDialogHidden, setShutdownDialogHidden] = useState(true);
    const [changeSettingDialogHidden, setChangeSettingDialogHidden] = useState(true);
    const [vscodeInstanceName, setVscodeInstanceName] = useState<string>();

    const isWindowsEnv = environment.skuName?.toLowerCase().includes('windows');
    const serviceUri = `https://${window.location.hostname}/api/v1/`;
    const vsComServiceUri = btoa(serviceUri);
    const { t: translation } = useTranslation();

    let items = [
        {
            key: 'open-vscode',
            iconProps: { iconName: 'OpenInNewWindow' },
            name: translation('openInVSCode'),
            disabled: environmentIsALie(environment) || isNotConnectable(environment),
            onClick: async () => {
                try {
                    await tryOpeningUrl(environment, 'vscode');
                } catch {
                    setVscodeInstanceName('VS Code');
                    setUnsuccessfulUrlDialogHidden(false);
                }
            },
        },
        {
            key: 'open-vscode-insiders',
            iconProps: { iconName: 'OpenInNewWindow' },
            name: translation('openInVSCodeInsiders'),
            disabled: environmentIsALie(environment) || isNotConnectable(environment),
            onClick: async () => {
                try {
                    await tryOpeningUrl(environment, 'vscode-insiders');
                } catch {
                    setVscodeInstanceName('VS Code Insiders');
                    setUnsuccessfulUrlDialogHidden(false);
                }
            },
        },
        {
            key: 'open-web',
            iconProps: { iconName: 'PlugConnected' },
            name: translation('connect'),
            disabled: environmentIsALie(environment) || isNotConnectable(environment),
            href: `environment/${environment.id!}`,
        },
        {
            key: 'enable-editing',
            iconProps: { iconName: 'Edit' },
            name: translation('changeSettings'),
            disabled: environmentIsALie(environment) || !isInStableState(environment),
            onClick: () => {
                if (!environment.id) {
                    return;
                }

                if (environment.state === EnvironmentStateInfo.Available) {
                    setChangeSettingDialogHidden(false);
                } else {
                    enableEditing();
                }
            },
        },
        {
            key: 'shutdown',
            iconProps: { iconName: 'PowerButton' },
            name: translation('suspend'),
            disabled: environmentIsALie(environment) || isNotSuspendable(environment),
            onClick: () => {
                if (environment.id) {
                    setShutdownDialogHidden(false);
                }
            },
        },
        {
            key: 'delete',
            iconProps: { iconName: 'Delete' },
            name: isSelfHostedEnvironment(environment) ? translation('unregister') : translation('delete'),
            disabled: environmentIsALie(environment),
            onClick: () => {
                if (environment.id) setDeleteDialogHidden(false);
            },
        },
    ];

    if (isWindowsEnv) {
        items = [
            {
                key: 'open-vs',
                iconProps: { iconName: 'OpenInNewWindow' },
                name: translation('openInVisualStudio'),
                disabled: environmentIsALie(environment) || isNotConnectable(environment),
                onClick: async () => {
                    const link = `https://visualstudio.microsoft.com/services/visual-studio-online/start-vs/?environmentId=${environment.id}&serviceUri=${vsComServiceUri}`;
                    window.open(link, '_blank');
                },
            },
            ...items,
        ];
    }
    return (
        <>
            <IconButton
                iconProps={{}}
                title={translation('More')}
                menuIconProps={{ iconName: 'MoreVertical', style: { fontSize: '1.6rem' } }}
                menuProps={{
                    isBeakVisible: false,
                    items,
                }}
            />
            <ChangeSettingsDialog
                environment={environment}
                shutdownEnvironment={shutdownEnvironment}
                enableEditing={enableEditing}
                // tslint:disable-next-line: react-this-binding-issue
                close={() => {
                    setChangeSettingDialogHidden(true);
                }}
                hidden={changeSettingDialogHidden}
            />
            <ShutdownDialog
                environment={environment}
                shutdownEnvironment={shutdownEnvironment}
                // tslint:disable-next-line: react-this-binding-issue
                close={() => {
                    setShutdownDialogHidden(true);
                }}
                hidden={shutdownDialogHidden}
            />
            <DeleteDialog
                environment={environment}
                deleteEnvironment={deleteEnvironment}
                // tslint:disable-next-line: react-this-binding-issue
                cancel={() => {
                    setDeleteDialogHidden(true);
                }}
                hidden={deleteDialogHidden}
            />
            <UnsuccessfulUrlDialog
                vscodeName={vscodeInstanceName}
                // tslint:disable-next-line: react-this-binding-issue
                accept={() => {
                    setUnsuccessfulUrlDialogHidden(true);
                }}
                hidden={unsuccessfulUrlDialogHidden}
            />
        </>
    );
};

type DeleteDialogProps = {
    deleteEnvironment: (...params: Parameters<typeof deleteEnvironment>) => void;
    cancel: () => void;
    environment: ILocalEnvironment;
    hidden: boolean;
};

function DeleteDialog({ deleteEnvironment, environment, cancel, hidden }: DeleteDialogProps) {
    const { t: translation } = useTranslation();
    const title = injectMessageParameters(translation('deleteCodespaceTitle'), environment.friendlyName);
    const subText = injectMessageParameters(translation('deleteCodespaceSubText'), environment.friendlyName);

    return (
        <Dialog
            hidden={hidden}
            dialogContentProps={{
                type: DialogType.normal,
                title,
                subText,
            }}
            modalProps={{
                isBlocking: true,
                styles: { main: { maxWidth: 450 } },
            }}
        >
            <DialogFooter>
                <PrimaryButton
                    onClick={
                        // tslint:disable-next-line: react-this-binding-issue
                        () => deleteEnvironment(environment.id!)
                    }
                    text={translation('delete')}
                />
                <DefaultButton onClick={cancel} text='Cancel' />
            </DialogFooter>
        </Dialog>
    );
}

type ShutdownDialogProps = {
    shutdownEnvironment: (...params: Parameters<typeof shutdownEnvironment>) => void;
    close: () => void;
    environment: ILocalEnvironment;
    hidden: boolean;
};

function ShutdownDialog({ shutdownEnvironment, environment, close, hidden }: ShutdownDialogProps) {
    const suspendEnvironment = useCallback(() => {
        if (!environment.id) {
            return;
        }

        shutdownEnvironment(environment as IEnvironment);
        close();
    }, [shutdownEnvironment, close, environment.id, environment.state]);
    const { t: translation } = useTranslation();
    const title = injectMessageParameters(translation('suspendCodespaceTitle'), environment.friendlyName);
    const subText = injectMessageParameters(translation('suspendCodespaceSubText'), environment.friendlyName);

    return (
        <Dialog
            hidden={hidden}
            dialogContentProps={{
                type: DialogType.normal,
                title,
                subText,
            }}
            modalProps={{
                isBlocking: true,
                styles: { main: { maxWidth: 450 } },
            }}
        >
            <DialogFooter>
                <PrimaryButton onClick={suspendEnvironment} text={translation('suspend')} />
                <DefaultButton onClick={close} text={translation('cancel')} />
            </DialogFooter>
        </Dialog>
    );
}

type ChangeSettingsDialogProps = {
    shutdownEnvironment: (...params: Parameters<typeof shutdownEnvironment>) => void;
    enableEditing: () => void;
    close: () => void;
    environment: ILocalEnvironment;
    hidden: boolean;
};

function ChangeSettingsDialog({
    shutdownEnvironment,
    enableEditing,
    environment,
    close,
    hidden,
}: ChangeSettingsDialogProps) {
    const suspendEnvironment = useCallback(() => {
        if (!environment.id) {
            return;
        }

        shutdownEnvironment(environment as IEnvironment);
        enableEditing();
        close();
    }, [shutdownEnvironment, close, environment.id, environment.state]);
    const { t: translation } = useTranslation();
    const title = injectMessageParameters(translation('suspendCodespaceTitle'), environment.friendlyName);
    const subText = injectMessageParameters(translation('changeSettingsSubText'), environment.friendlyName);

    return (
        <Dialog
            hidden={hidden}
            dialogContentProps={{
                type: DialogType.normal,
                title,
                subText,
            }}
            modalProps={{
                isBlocking: true,
                styles: { main: { maxWidth: 450 } },
            }}
        >
            <DialogFooter>
                <PrimaryButton onClick={suspendEnvironment} text={translation('suspend')} />
                <DefaultButton onClick={close} text={translation('cancel')} />
            </DialogFooter>
        </Dialog>
    );
}

type UnsuccessfulUrlDialogProps = {
    accept: () => void;
    hidden: boolean;
    vscodeName: string | undefined;
};

function UnsuccessfulUrlDialog({ accept, hidden, vscodeName }: UnsuccessfulUrlDialogProps) {
    const { t: translation } = useTranslation();
    const title = injectMessageParameters(translation('couldNotFindVSCodeInstallation'), vscodeName);
    return (
        <Dialog
            hidden={hidden}
            dialogContentProps={{
                type: DialogType.normal,
                title,
            }}
            modalProps={{
                isBlocking: true,
                styles: { main: { maxWidth: 450 } },
            }}
        >
            <DialogFooter>
                <DefaultButton onClick={accept} text={translation('accept')} />
            </DialogFooter>
        </Dialog>
    );
}

type EnvironmentErrorsDialogProps = {
    title: string;
    clearErrorMessages: () => void;
    errorMessages: string[] | undefined;
};

function EnvironmentErrorsDialog({
    title,
    errorMessages,
    clearErrorMessages,
}: EnvironmentErrorsDialogProps) {
    if (!errorMessages) {
        return null;
    }

    const messageBars = errorMessages.map((msg) => (
        <MessageBar
            messageBarType={MessageBarType.error}
            isMultiline={true}
            className='environment-card__error-dialog-content-message'
            key={msg}
        >
            {msg}
        </MessageBar>
    ));

    return (
        <Dialog
            hidden={false}
            dialogContentProps={{
                type: DialogType.close,
                title,
            }}
            modalProps={{
                isBlocking: true,
                styles: { main: { maxWidth: 450 } },
            }}
            onDismiss={clearErrorMessages}
        >
            <div className='environment-card__error-dialog-content'>{messageBars}</div>
        </Dialog>
    );
}

const getSkuDisplayName = (
    selectedPlan: ActivePlanInfo,
    skuName: string,
    translation: TFunction,
    defaultDisplayName?: string
) => {
    if (!selectedPlan.availableSkus) {
        return skuName;
    }
    const sku = selectedPlan.availableSkus.find((sku) => sku.name === skuName);
    return sku ? getSkuSpecLabel(sku, translation) : defaultDisplayName || skuName;
};

const suspendTimeoutToDisplayName = (timeoutInMinutes: number = 0, translationFunc: TFunction) => {
    if (timeoutInMinutes === 0) {
        return translationFunc('never');
    } else if (timeoutInMinutes < 60) {
        return injectMessageParameters(translationFunc('afterMinutes'), timeoutInMinutes);
    } else {
        const timeoutInHours = timeoutInMinutes / 60;
        const message = translationFunc(timeoutInHours === 1 ? 'afterHour' : 'afterHours');
        return injectMessageParameters(message, timeoutInHours);
    }
};

type EnvironmentNameProps = {
    environment: ILocalEnvironment;
};
function EnvironmentName({ environment }: Readonly<EnvironmentNameProps>) {
    const shortFriendlyName = getShortFriendlyName(environment.friendlyName);
    const environmentNameText = (
        <Stack horizontal verticalAlign='center'>
            <Icon
                iconName='ThisPC'
                style={{ color: getTheme().palette.themePrimary }}
                className='environment-card__environment-icon'
            />
            <div className='environment-card__environment-name'>
                <Text
                    variant='large'
                    title={
                        shortFriendlyName === environment.friendlyName
                            ? undefined
                            : environment.friendlyName
                    }
                >
                    {shortFriendlyName}
                </Text>
            </div>
            <Status environment={environment} />
        </Stack>
    );

    if (environmentIsALie(environment) || isNotConnectable(environment)) {
        return environmentNameText;
    }

    return (
        <Link className='environment-card__environment-link' href={`environment/${environment.id}`}>
            {environmentNameText}
        </Link>
    );
}

type ApplyEnvironmentSettingsChangeButtonProps = {
    environment: ILocalEnvironment;
    disabled: boolean;
    settingsChangeState: EnvironmentSettingsChangeState;
    submitSettingsUpdate: () => void;
};

function ApplyEnvironmentSettingsChangeButton({
    environment,
    settingsChangeState,
    submitSettingsUpdate,
    disabled,
}: ApplyEnvironmentSettingsChangeButtonProps) {
    const { t: translation } = useTranslation();
    const loader = (
        <Loader message={''} className={'environment-card__change-settings-apply-button-loader'} translation={translation}/>
    );
    let text: string;
    let isLoading: boolean;

    if (environment.state !== EnvironmentStateInfo.Shutdown) {
        text = translation('suspendingProgress');
        isLoading = true;
    } else if (settingsChangeState === EnvironmentSettingsChangeState.Submitting) {
        text = translation('submitting');
        isLoading = true;
    } else {
        text = translation('apply');
        isLoading = false;
    }

    return (
        <PrimaryButton
            onClick={submitSettingsUpdate}
            className={'environment-card__change-settings-button'}
            disabled={disabled || environment.state !== EnvironmentStateInfo.Shutdown}
        >
            {isLoading ? loader : null}
            {text}
        </PrimaryButton>
    );
}

function getShortFriendlyName(friendlyName: string): string {
    if (friendlyName.length > friendlyNameDisplayLength) {
        return `${friendlyName.substr(0, friendlyNameDisplayLength)}...`;
    }

    return friendlyName;
}

type FooterProps = {
    environment: ILocalEnvironment;
    settingsChangeState: EnvironmentSettingsChangeState;

    deleteEnvironment: (id: string) => void;
    shutdownEnvironment: (environmentInfo: IEnvironment) => void;
    submitSettingsUpdate: () => void;

    startEditing: () => void;
    cancelEditing: () => void;
};

function Footer({
    environment,
    settingsChangeState,
    deleteEnvironment,
    shutdownEnvironment,
    submitSettingsUpdate,
    startEditing,
    cancelEditing,
}: FooterProps) {
    const { t: translation } = useTranslation();
    if (settingsChangeState === EnvironmentSettingsChangeState.Display) {
        return (
            <Actions
                environment={environment}
                deleteEnvironment={deleteEnvironment}
                shutdownEnvironment={shutdownEnvironment}
                enableEditing={startEditing}
            />
        );
    } else {
        const buttonsDisabled = settingsChangeState !== EnvironmentSettingsChangeState.Editing;

        return (
            <>
                <ApplyEnvironmentSettingsChangeButton
                    environment={environment}
                    settingsChangeState={settingsChangeState}
                    disabled={buttonsDisabled}
                    submitSettingsUpdate={submitSettingsUpdate}
                />
                <DefaultButton
                    text={translation('cancel')}
                    onClick={cancelEditing}
                    className={'environment-card__change-settings-button'}
                    disabled={buttonsDisabled}
                />
            </>
        );
    }
}

// tslint:disable-next-line: max-func-body-length
export function EnvironmentCard(props: EnvironmentCardProps) {
    const [errorMessages, setErrorMessages] = useState(undefined as string[] | undefined);
    const [settingsChangeErrors, setSettingsChangeErrors] = useState(
        undefined as string[] | undefined
    );
    const [settingsChangeState, setSettingsChangeState] = useState(
        EnvironmentSettingsChangeState.Display
    );
    const [availableSettings, setAvailableSettings] = useState(
        undefined as EnvironmentSettingsAllowedUpdates | undefined
    );

    const clearErrorMessages = useCallback(() => {
        setErrorMessages(undefined);
    }, []);

    const clearSettingsChangeErrorMessages = useCallback(() => {
        setSettingsChangeErrors(undefined);
    }, []);

    const selectedPlan = useSelector((state: ApplicationState) => state.plans.selectedPlan);
    const { t: translation } = useTranslation();
    let details: Detail[] = [];

    details.push({
        key: translation('created'),
        value: props.environment.created,
        text: moment(props.environment.created).format('LLLL'),
        editable: false,
    });

    if (props.environment.seed && props.environment.seed.moniker) {
        details.push({
            key: translation('repository'),
            value: props.environment.seed.moniker,
            text: props.environment.seed.moniker,
            editable: false,
        });
    }

    const isSelfHosted = isSelfHostedEnvironment(props.environment!);

    const editOptions = getDropdownOptionForSettingsUpdates(
        availableSettings,
        props.environment!,
        selectedPlan!,
        translation,
    );

    const [skuName, setSkuName] = useState(props.environment.skuName);
    details.push({
        key: translation('instance'),
        value: skuName,
        text: getSkuDisplayName(selectedPlan!, skuName, translation),
        editable: !isSelfHosted,
        editOptions: editOptions.skuEditOptions,
        setValue: (opt) => opt && setSkuName(String(opt.key)),
    });

    const [autoShutdownDelayMinutes, setAutoShutdownDelayMinutes] = useState(
        props.environment.autoShutdownDelayMinutes!
    );
    if (!isSelfHosted) {
        details.push({
            key: translation('suspend'),
            value: autoShutdownDelayMinutes,
            text: suspendTimeoutToDisplayName(autoShutdownDelayMinutes, translation),
            editable: !isSelfHosted,
            editOptions: editOptions.autoShutdownDelayEditOptions,
            setValue: (opt) => opt && setAutoShutdownDelayMinutes(Number(opt.key)),
        });
    }

    const startEditing = useCallback(() => {
        setSettingsChangeState(EnvironmentSettingsChangeState.Editing);
    }, [setSettingsChangeState]);

    const needsToFetchSettings =
        settingsChangeState === EnvironmentSettingsChangeState.Editing &&
        !isDefined(availableSettings);

    useEffect(() => {
        if (!needsToFetchSettings) {
            return;
        }

        const allowedUpdatesSignal = Signal.from(
            getAllowedEnvironmentSettingsChanges(props.environment.id!)
        );

        allowedUpdatesSignal.promise.then((updates) => {
            setAvailableSettings(updates);
        });

        return () => {
            allowedUpdatesSignal.cancel();
        };
    }, [needsToFetchSettings]);

    const stopEditing = useCallback(() => {
        setSettingsChangeState(EnvironmentSettingsChangeState.Display);
        setAvailableSettings(undefined);
    }, [setSettingsChangeState, setAvailableSettings]);

    const resetEditedSettings = useCallback(() => {
        setSkuName(props.environment.skuName);
        setAutoShutdownDelayMinutes(props.environment.autoShutdownDelayMinutes!);
    }, [setSkuName, setAutoShutdownDelayMinutes]);

    const updateRequest = buildEnvironmentSettingsUpdateRequest(
        props.environment,
        skuName,
        autoShutdownDelayMinutes
    );

    const doSettingsUpdate = useCallback(() => {
        if (updateRequest) {
            setSettingsChangeState(EnvironmentSettingsChangeState.Submitting);
        } else {
            stopEditing();
        }
    }, [setSettingsChangeState, stopEditing, updateRequest]);

    const shouldMakeUpdateRequest =
        settingsChangeState === EnvironmentSettingsChangeState.Submitting;

    useEffect(() => {
        if (!shouldMakeUpdateRequest || !updateRequest) {
            return;
        }

        const updateSignal = Signal.from(
            updateEnvironmentSettings(props.environment.id!, updateRequest)
        );

        updateSignal.promise.then(
            () => {
                stopEditing();
            },
            async (err) => {
                if (err instanceof ServiceResponseError) {
                    const codes = (await err.response.json()) as EnvironmentErrorCodes[];
                    setSettingsChangeErrors(codes.map((code) => {
                        return environmentErrorCodeToString(code, translation);
                    }));
                } else {
                    setSettingsChangeErrors([err.toString()]);
                }

                startEditing();
            }
        );

        return () => {
            updateSignal.cancel();
        };
    }, [shouldMakeUpdateRequest]);

    const indicator = isActivating(props.environment) ? (
        <ProgressIndicator className={'environment-card__thin-progress'} />
    ) : (
        <ThinSeparator />
    );

    return (
        <Stack
            style={{
                boxShadow: getTheme().effects.elevation4,
            }}
            className='environment-card'
        >
            <EnvironmentName environment={props.environment} />

            {indicator}

            <Stack style={{ flexGrow: 1 }}>
                <Details details={details} settingsChangeState={settingsChangeState} />
            </Stack>
            <ThinSeparator />
            <Stack horizontal>
                <Stack grow={1} />
                <Footer
                    environment={props.environment!}
                    shutdownEnvironment={props.shutdownEnvironment}
                    deleteEnvironment={props.deleteEnvironment}
                    settingsChangeState={settingsChangeState}
                    submitSettingsUpdate={doSettingsUpdate}
                    startEditing={startEditing}
                    // tslint:disable-next-line: react-this-binding-issue
                    cancelEditing={() => {
                        stopEditing();
                        resetEditedSettings();
                    }}
                />
            </Stack>
            <EnvironmentErrorsDialog
                title={translation('weAreGettingThingsReady')}
                errorMessages={errorMessages}
                clearErrorMessages={clearErrorMessages}
            />
            <EnvironmentErrorsDialog
                title={translation('failedToUpdateSettings')}
                errorMessages={settingsChangeErrors}
                clearErrorMessages={clearSettingsChangeErrorMessages}
            />
        </Stack>
    );
}

export function getDropdownOptionForSettingsUpdates(
    availableSettings: EnvironmentSettingsAllowedUpdates | undefined,
    environment: ILocalEnvironment,
    selectedPlan: ActivePlanInfo,
    translateFunc: TFunction,
): {
    skuEditOptions?: IDropdownOption[];
    autoShutdownDelayEditOptions?: IDropdownOption[];
} {
    if (!availableSettings) {
        return {};
    }

    const skuEditOptions =
        availableSettings.allowedSkus &&
        availableSettings.allowedSkus.map((sku) => ({
            key: sku.name,
            text: getSkuDisplayName(selectedPlan, sku.name, translateFunc, sku.displayName),
        }));

    addCurrentSettingValueOptionIfNotExists(skuEditOptions, {
        key: environment.skuName,
        text: getSkuDisplayName(selectedPlan, environment.skuName, translateFunc, environment.skuDisplayName),
    });

    const autoShutdownDelayEditOptions =
        availableSettings.allowedAutoShutdownDelayMinutes &&
        availableSettings.allowedAutoShutdownDelayMinutes.map((delay) => ({
            key: delay,
            text: suspendTimeoutToDisplayName(delay, translateFunc),
        }));

    addCurrentSettingValueOptionIfNotExists(autoShutdownDelayEditOptions, {
        key: environment.autoShutdownDelayMinutes,
        text: suspendTimeoutToDisplayName(environment.autoShutdownDelayMinutes, translateFunc),
    });

    return {
        skuEditOptions,
        autoShutdownDelayEditOptions,
    };
}

function addCurrentSettingValueOptionIfNotExists(
    options: { key: any; text: string }[],
    currentValueOption: { key: any; text: string }
) {
    if (!options || !options.length) {
        return;
    }

    // If current value isn't in the allowed list, manually add the option
    // When submitting, it will be ignored
    if (!options.find((opt) => opt.key === currentValueOption.key)) {
        options.unshift(currentValueOption);
    }
}

export function buildEnvironmentSettingsUpdateRequest(
    environment: ILocalEnvironment,
    skuName: string | undefined,
    autoShutdownDelayMinutes: number | undefined
): EnvironmentSettingsUpdate | null {
    const updateRequest: EnvironmentSettingsUpdate = {};

    let anyUpdate = false;

    if (skuName && skuName !== environment.skuName) {
        updateRequest.skuName = skuName;
        anyUpdate = true;
    }

    if (
        autoShutdownDelayMinutes &&
        autoShutdownDelayMinutes !== environment.autoShutdownDelayMinutes
    ) {
        updateRequest.autoShutdownDelayMinutes = autoShutdownDelayMinutes;
        anyUpdate = true;
    }

    return anyUpdate ? updateRequest : null;
}
