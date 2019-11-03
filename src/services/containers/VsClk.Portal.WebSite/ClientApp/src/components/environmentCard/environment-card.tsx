import React, {
    useState,
    useEffect,
    useCallback,
    useMemo,
    Fragment,
    MouseEvent,
    KeyboardEvent,
} from 'react';
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

import { ILocalCloudEnvironment, StateInfo } from '../../interfaces/cloudenvironment';
import { deleteEnvironment } from '../../actions/deleteEnvironment';
import { shutdownEnvironment } from '../../actions/shutdownEnvironment';
import {
    environmentIsALie,
    isNotConnectable,
    isNotSuspendable,
    isSelfHostedEnvironment,
    isActivating,
    getSkuSpecLabel,
} from '../../utils/environmentUtils';
import { tryOpeningUrl } from '../../utils/vscodeProtocolUtil';
import './environment-card.css';
import { connectEnvironment } from '../../actions/connectEnvironment';
import { createTrace } from '../../utils/createTrace';
import { useSelector } from 'react-redux';
import { ApplicationState } from '../../reducers/rootReducer';
import { ActivePlanInfo } from '../../reducers/plans-reducer';
import { Spinner, SpinnerSize, SpinnerType } from 'office-ui-fabric-react/lib/Spinner';
import { Signal, CancellationError } from '../../utils/signal';
import { MessageBarType, MessageBar } from 'office-ui-fabric-react/lib/MessageBar';
import { CancellationTokenSource, CancellationToken } from 'vscode-jsonrpc';
import { isDefined } from '../../utils/isDefined';
import { isMacOs } from '../../utils/detection';

const trace = createTrace('environment-card');
export interface EnvironmentCardProps {
    className?: string;
    environment: ILocalCloudEnvironment;
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

function Details({ details }: { details: { key: string; value: string }[] }) {
    const rows = details.map((d) => (
        <div className='environment-card__details-table-row' key={`${d.key}${d.value}`}>
            <div className='environment-card__details-table-cell environment-card__details-label'>
                <Text nowrap block style={{ color: NeutralColors.gray160 }}>
                    {d.key}
                </Text>
            </div>
            <div className='environment-card__details-table-cell environment-card__details-value'>
                <Text nowrap block style={{ color: NeutralColors.black }}>
                    {d.value}
                </Text>
            </div>
        </div>
    ));

    return <div className='environment-card__details-table'>{rows}</div>;
}

function stateToDisplayName(state: StateInfo) {
    switch (state) {
        case StateInfo.Provisioning:
            return 'Creating';
        case StateInfo.Failed:
            return 'Failed to Create';
        case StateInfo.Shutdown:
            return 'Suspended';
        case StateInfo.ShuttingDown:
            return 'Suspending';
        default:
            return state;
    }
}

function Status({ environment }: { environment: ILocalCloudEnvironment }) {
    let backgroundColor;
    let color;

    switch (environment.state) {
        case StateInfo.Available:
            backgroundColor = '#6BB700';
            color = NeutralColors.gray160;
            break;
        case StateInfo.Deleted:
        case StateInfo.Failed:
            backgroundColor = SharedColors.red20;
            color = NeutralColors.gray10;
            break;
        default:
            backgroundColor = NeutralColors.gray20;
            color = NeutralColors.gray160;
    }

    return (
        <Text
            block
            className='environment-card__status'
            style={{
                color,
                backgroundColor,
            }}
        >
            {stateToDisplayName(environment.state)}
        </Text>
    );
}

type ActionProps = {
    environment: ILocalCloudEnvironment;
    deleteEnvironment: (...params: Parameters<typeof deleteEnvironment>) => void;
    shutdownEnvironment: (...params: Parameters<typeof shutdownEnvironment>) => void;
    connect: (event: MouseEvent | KeyboardEvent | undefined) => void;
};

// tslint:disable-next-line: max-func-body-length
const Actions = ({
    environment,
    deleteEnvironment,
    shutdownEnvironment,
    connect: connectToEnvironment,
}: ActionProps) => {
    const [deleteDialogHidden, setDeleteDialogHidden] = useState(true);
    const [unsuccessfulUrlDialogHidden, setUnsuccessfulUrlDialogHidden] = useState(true);
    const [shutdownDialogHidden, setShutdownDialogHidden] = useState(true);
    const [vscodeInstanceName, setVscodeInstanceName] = useState();

    return (
        <>
            <IconButton
                iconProps={{}}
                title='More'
                menuIconProps={{ iconName: 'MoreVertical', style: { fontSize: '1.6rem' } }}
                menuProps={{
                    isBeakVisible: false,
                    items: [
                        {
                            key: 'open-vscode',
                            iconProps: { iconName: 'OpenInNewWindow' },
                            name: 'Open in VS Code',
                            disabled:
                                environmentIsALie(environment) || isNotConnectable(environment),
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
                            name: 'Open in VS Code Insiders',
                            disabled:
                                environmentIsALie(environment) || isNotConnectable(environment),
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
                            name: 'Connect',
                            disabled:
                                environmentIsALie(environment) || isNotConnectable(environment),
                            onClick: connectToEnvironment,
                        },
                        {
                            key: 'shutdown',
                            iconProps: { iconName: 'PowerButton' },
                            name: 'Suspend',
                            disabled:
                                environmentIsALie(environment) || isNotSuspendable(environment),
                            onClick: () => {
                                if (environment.id) setShutdownDialogHidden(false);
                            },
                        },
                        {
                            key: 'delete',
                            iconProps: { iconName: 'Delete' },
                            name: isSelfHostedEnvironment(environment) ? 'Unregister' : 'Delete',
                            disabled: environmentIsALie(environment),
                            onClick: () => {
                                if (environment.id) setDeleteDialogHidden(false);
                            },
                        },
                    ],
                }}
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
    environment: ILocalCloudEnvironment;
    hidden: boolean;
};

function DeleteDialog({ deleteEnvironment, environment, cancel, hidden }: DeleteDialogProps) {
    return (
        <Dialog
            hidden={hidden}
            dialogContentProps={{
                type: DialogType.normal,
                title: `Delete environment ${environment.friendlyName}`,
                subText: `You are about to delete the ${environment.friendlyName}. Are you sure?`,
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
                    text='Delete'
                />
                <DefaultButton onClick={cancel} text='Cancel' />
            </DialogFooter>
        </Dialog>
    );
}

type ShutdownDialogProps = {
    shutdownEnvironment: (...params: Parameters<typeof shutdownEnvironment>) => void;
    close: () => void;
    environment: ILocalCloudEnvironment;
    hidden: boolean;
};

function ShutdownDialog({ shutdownEnvironment, environment, close, hidden }: ShutdownDialogProps) {
    const suspendEnvironment = useCallback(() => {
        if (!environment.id) {
            return;
        }

        shutdownEnvironment(environment.id);
        close();
    }, [shutdownEnvironment, close, environment.id]);

    return (
        <Dialog
            hidden={hidden}
            dialogContentProps={{
                type: DialogType.normal,
                title: `Suspend environment ${environment.friendlyName}`,
                subText: `You are about to suspend ${environment.friendlyName}. Are you sure?`,
            }}
            modalProps={{
                isBlocking: true,
                styles: { main: { maxWidth: 450 } },
            }}
        >
            <DialogFooter>
                <PrimaryButton onClick={suspendEnvironment} text='Suspend' />
                <DefaultButton onClick={close} text='Cancel' />
            </DialogFooter>
        </Dialog>
    );
}

type UnsuccessfulUrlDialogProps = {
    accept: () => void;
    hidden: boolean;
    vscodeName: string;
};

function UnsuccessfulUrlDialog({ accept, hidden, vscodeName }: UnsuccessfulUrlDialogProps) {
    return (
        <Dialog
            hidden={hidden}
            dialogContentProps={{
                type: DialogType.normal,
                title: `Could not find ${vscodeName} installation.`,
            }}
            modalProps={{
                isBlocking: true,
                styles: { main: { maxWidth: 450 } },
            }}
        >
            <DialogFooter>
                <DefaultButton onClick={accept} text='Accept' />
            </DialogFooter>
        </Dialog>
    );
}

type EnvironmentConnectionFailedDialogProps = {
    clearErrorMessage: () => void;
    errorMessage: string | undefined;
};

function EnvironmentConnectionFailedDialog({
    errorMessage,
    clearErrorMessage,
}: EnvironmentConnectionFailedDialogProps) {
    if (!errorMessage) {
        return null;
    }

    return (
        <Dialog
            hidden={false}
            dialogContentProps={{
                type: DialogType.close,
                title: 'We are getting things ready.',
            }}
            modalProps={{
                isBlocking: true,
                styles: { main: { maxWidth: 450 } },
            }}
            onDismiss={clearErrorMessage}
        >
            <div className='environment-card__connection-dialog-content'>
                <MessageBar messageBarType={MessageBarType.error} isMultiline={true}>
                    {errorMessage}
                </MessageBar>
            </div>
        </Dialog>
    );
}

const getSkuDisplayName = (selectedPlan: ActivePlanInfo, environment: ILocalCloudEnvironment) => {
    const skuName = environment.skuName;
    if (!selectedPlan.availableSkus) {
        return skuName;
    }
    const sku = selectedPlan.availableSkus.find((sku) => sku.name === skuName);
    return sku ? getSkuSpecLabel(sku) : environment.skuDisplayName || skuName;
};

const suspendTimeoutToDisplayName = (timeoutInMinutes: number = 0) => {
    if (timeoutInMinutes === 0) {
        return 'Never';
    } else if (timeoutInMinutes < 60) {
        return `After ${timeoutInMinutes} minutes`;
    } else {
        const timeoutInHours = timeoutInMinutes / 60;
        return `After ${timeoutInHours} hours`;
    }
};

type EnvironmentNameProps = {
    environment: ILocalCloudEnvironment;
    connect: () => void;
};
function EnvironmentName({ environment, connect }: Readonly<EnvironmentNameProps>) {
    const environmentNameText = (
        <Stack horizontal verticalAlign='center'>
            <Icon
                iconName='ThisPC'
                style={{ color: getTheme().palette.themePrimary }}
                className='environment-card__environment-icon'
            />
            <div className='environment-card__environment-name'>
                <Text variant='large'>{environment.friendlyName}</Text>
            </div>
            <Status environment={environment} />
        </Stack>
    );

    if (environmentIsALie(environment) || isNotConnectable(environment)) {
        return environmentNameText;
    }

    return (
        <Link className='environment-card__environment-link' onClick={connect}>
            {environmentNameText}
        </Link>
    );
}

let currentConnectingEnvironment: Signal<void> | undefined;

// tslint:disable-next-line: max-func-body-length
export function EnvironmentCard(props: EnvironmentCardProps) {
    const [errorMessage, setErrorMessage] = useState(undefined as string | undefined);
    const clearErrorMessage = useCallback(() => {
        setErrorMessage(undefined);
    }, []);

    const connectEnv = useCallback(
        async (event?: MouseEvent | KeyboardEvent | undefined) => {
            if (!props.environment.id) {
                return;
            }

            if (event) {
                // We'll keep the react event around for async.
                event.persist();
            }

            let shouldOpenInNewTab = false;
            if (event && isDefined((event as MouseEvent).buttons)) {
                const isMiddleClick = (event as MouseEvent).button === 1;
                const isMacOpenNewTab =
                    isMacOs() && (event as MouseEvent).button === 0 && event.metaKey;
                const isWinOpenNewTab =
                    !isMacOs() && (event as MouseEvent).button === 0 && event.ctrlKey;
                shouldOpenInNewTab = isMiddleClick || isMacOpenNewTab || isWinOpenNewTab;
            }

            if (currentConnectingEnvironment) {
                currentConnectingEnvironment.cancel();
            }

            const cancellationTokenSource = new CancellationTokenSource();
            currentConnectingEnvironment = Signal.from(
                connect(
                    props.environment.id,
                    props.environment.state,
                    cancellationTokenSource.token
                )
            );

            await currentConnectingEnvironment.promise.then(
                () => {},
                (err) => {
                    if (err instanceof CancellationError) {
                        cancellationTokenSource.cancel();
                        return;
                    }

                    throw err;
                }
            );

            async function connect(
                id: string,
                state: StateInfo,
                cancellationToken: CancellationToken
            ) {
                try {
                    let result = await connectEnvironment(id, state);
                    trace.verbose('Connect to environment done.', result);

                    if (cancellationToken.isCancellationRequested) {
                        return;
                    }

                    currentConnectingEnvironment = undefined;

                    if (result) {
                        const fullUrl = new URL(
                            `/environment/${id}`,
                            window.location.origin
                        ).toString();

                        if (shouldOpenInNewTab) {
                            window.open(fullUrl, '_blank');
                        } else {
                            window.location.assign(fullUrl);
                        }
                    }
                } catch (err) {
                    setErrorMessage(err.message);
                }
            }
        },
        [props.environment.id]
    );

    const selectedPlan = useSelector((state: ApplicationState) => state.plans.selectedPlan);
    let details = [];
    details.push({ key: 'Created', value: moment(props.environment.created).format('LLLL') });
    if (props.environment.seed && props.environment.seed.moniker) {
        details.push({ key: 'Repository', value: props.environment.seed.moniker });
    }

    details.push({
        key: 'Instance',
        value: getSkuDisplayName(selectedPlan!, props.environment!),
    });
    if (!isSelfHostedEnvironment(props.environment!)) {
        details.push({
            key: 'Suspend',
            value: suspendTimeoutToDisplayName(props.environment.autoShutdownDelayMinutes),
        });
    }

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
            <EnvironmentName environment={props.environment} connect={connectEnv} />

            {indicator}

            <Stack style={{ flexGrow: 1 }}>
                <Details details={details} />
            </Stack>
            <ThinSeparator />
            <Stack horizontal>
                <Stack grow={1} />
                <Actions
                    environment={props.environment}
                    deleteEnvironment={props.deleteEnvironment}
                    shutdownEnvironment={props.shutdownEnvironment}
                    connect={connectEnv}
                />
            </Stack>
            <EnvironmentConnectionFailedDialog
                errorMessage={errorMessage}
                clearErrorMessage={clearErrorMessage}
            />
        </Stack>
    );
}
