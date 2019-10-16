import React, { useState } from 'react';
import moment from 'moment';

import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { Text } from 'office-ui-fabric-react/lib/Text';
import { getTheme } from 'office-ui-fabric-react/lib/Styling';
import { IconButton, PrimaryButton, DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Dialog, DialogType, DialogFooter } from 'office-ui-fabric-react/lib/Dialog';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { SharedColors, NeutralColors } from '@uifabric/fluent-theme/lib/fluent/FluentColors';

import { ILocalCloudEnvironment, StateInfo, ICloudEnvironment } from '../../interfaces/cloudenvironment';
import { deleteEnvironment } from '../../actions/deleteEnvironment';
import { shutdownEnvironment } from '../../actions/shutdownEnvironment';
import { environmentIsALie, isNotAvailable, isNotConnectable } from '../../utils/environmentUtils';
import { createUniqueId } from '../../dependencies';
import { tryOpeningUrl } from '../../utils/vscodeProtocolUtil';
import './environment-card.css';
import { withRouter, match } from 'react-router-dom';
import { History, Location } from 'history';
import { connectEnvironment } from '../../actions/connectEnvironment';

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
            {environment.state}
        </Text>
    );
}

type ActionProps = {
    history: History;
    location: Location<{}>;
    match: match<{}>;
    environment: ILocalCloudEnvironment;
    deleteEnvironment: (...params: Parameters<typeof deleteEnvironment>) => void;
    shutdownEnvironment: (...params: Parameters<typeof shutdownEnvironment>) => void;
    connectEnvironment: (...name: Parameters<typeof connectEnvironment>) => Promise<ICloudEnvironment | undefined>;
};

const Actions = withRouter(({ environment, deleteEnvironment, shutdownEnvironment, connectEnvironment, history }: ActionProps) => {
    const [deleteDialogHidden, setDeleteDialogHidden] = useState(true);
    const [unsucessfullUrlDialogHidden, setUnsucessfullUrlDialogHidden] = useState(true);
    const [shutdownDialogHidden, setShutdownDialogHidden] = useState(true);
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
                            disabled: environmentIsALie(environment) || isNotConnectable(environment),
                            onClick: async () => {
                                if (environment.state === StateInfo.Shutdown) {
                                    await connectEnvironment(environment.id!, environment.state);
                                }

                                const url = `ms-vsonline.vsonline/connect?environmentId=${encodeURIComponent(
                                    environment.id!
                                )}&sessionPath=${
                                    environment.connection!.sessionPath
                                    }&correlationId=${createUniqueId()}`;

                                try {
                                    await tryOpeningUrl(`vscode-insiders://${url}`).catch(
                                        async () => {
                                            return await tryOpeningUrl(`vscode://${url}`);
                                        }
                                    );
                                } catch {
                                    setUnsucessfullUrlDialogHidden(false);
                                }
                            },
                        },
                        {
                            key: 'open-web',
                            iconProps: { iconName: 'PlugConnected' },
                            name: 'Connect',
                            disabled: environmentIsALie(environment) || isNotConnectable(environment),
                            onClick: async () => {
                                let result = await connectEnvironment(environment.id!, environment.state);
                                if (result) {
                                    history.push(`/environment/${environment.id}`);
                                }
                            },
                        },
                        {
                            key: 'shutdown',
                            iconProps: { iconName: 'PowerButton' },
                            name: 'Shutdown',
                            disabled: environmentIsALie(environment) || isNotAvailable(environment),
                            onClick: () => {
                                if (environment.id) setShutdownDialogHidden(false);
                            },
                        },
                        {
                            key: 'delete',
                            iconProps: { iconName: 'Delete' },
                            name: 'Delete',
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
            <UnsucessfullUrlDialog
                // tslint:disable-next-line: react-this-binding-issue
                accept={() => {
                    setUnsucessfullUrlDialogHidden(true);
                }}
                hidden={unsucessfullUrlDialogHidden}
            />
        </>
    );
});

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
    return (
        <Dialog
            hidden={hidden}
            dialogContentProps={{
                type: DialogType.normal,
                title: `Shutdown environment ${environment.friendlyName}`,
                subText: `You are about to shutdown ${environment.friendlyName}. Are you sure?`,
            }}
            modalProps={{
                isBlocking: true,
                styles: { main: { maxWidth: 450 } },
            }}
        >
            <DialogFooter>
                <PrimaryButton
                    onClick={
                        () => {
                            shutdownEnvironment(environment.id!);
                            close();
                        }
                    }
                    text='Shutdown'
                />
                <DefaultButton onClick={close} text='Cancel' />
            </DialogFooter>
        </Dialog>
    );
}

type UnsucessfullUrlDialogProps = {
    accept: () => void;
    hidden: boolean;
};

function UnsucessfullUrlDialog({ accept, hidden }: UnsucessfullUrlDialogProps) {
    return (
        <Dialog
            hidden={hidden}
            dialogContentProps={{
                type: DialogType.normal,
                title: `Could not find VSCode installation.`,
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

export function EnvironmentCard(props: EnvironmentCardProps) {
    const environmentNameText = <Text variant={'large'}>{props.environment.friendlyName}</Text>;
    const environmentName =
        environmentIsALie(props.environment) || isNotConnectable(props.environment)
            ? <div className='environment-card__environment-name'>{environmentNameText}</div>
            : (
                <Link
                    className='environment-card__environment-name'
                    onClick={async () => {
                        let result = await connectEnvironment(props.environment.id!, props.environment.state)
                        console.log(result);
                        if (result) {
                            window.location.replace(`environment/${props.environment.id!}`);
                        }
                    }}
                >
                    {environmentNameText}
                </Link >
            );

    let details = [];
    details.push({ key: 'Created', value: moment(props.environment.created).format('LLLL') });
    if (props.environment.seed && props.environment.seed.moniker) {
        details.push({ key: 'Repository', value: props.environment.seed.moniker });
    }

    return (
        <Stack
            style={{
                boxShadow: getTheme().effects.elevation4,
            }}
            className='environment-card'
        >
            <Stack horizontal verticalAlign='center'>
                <Icon
                    iconName='ThisPC'
                    style={{ color: getTheme().palette.themePrimary }}
                    className='environment-card__environment-icon'
                />
                {environmentName}
                <Status environment={props.environment} />
            </Stack>

            <ThinSeparator />
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
                    connectEnvironment={connectEnvironment}
                />
            </Stack>
        </Stack>
    );
}
