import React, { useState } from 'react';
import moment from 'moment';

import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { Text } from 'office-ui-fabric-react/lib/Text';
import { getTheme } from 'office-ui-fabric-react/lib/Styling';

import './environment-card.css';
import { SharedColors, NeutralColors } from '@uifabric/fluent-theme/lib/fluent/FluentColors';

import {
    ILocalCloudEnvironment,
    environmentIsALie,
    StateInfo,
    isNotAvailable,
} from '../../interfaces/cloudenvironment';

import { IconButton, PrimaryButton, DefaultButton } from 'office-ui-fabric-react/lib/Button';

import { deleteEnvironment } from '../../actions/deleteEnvironment';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Dialog, DialogType, DialogFooter } from 'office-ui-fabric-react/lib/Dialog';
import { Stack } from 'office-ui-fabric-react/lib/Stack';

export interface EnvironmentCardProps {
    className?: string;
    environment: ILocalCloudEnvironment;
    deleteEnvironment: (...params: Parameters<typeof deleteEnvironment>) => void;
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
    environment: ILocalCloudEnvironment;
    deleteEnvironment: (...params: Parameters<typeof deleteEnvironment>) => void;
};

const Actions = ({ environment, deleteEnvironment }: ActionProps) => {
    const [deleteDialogHidden, setDeleteDialogHidden] = useState(true);
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
                            disabled: environmentIsALie(environment) || isNotAvailable(environment),
                            onClick: () => {},
                        },
                        {
                            key: 'open-web',
                            iconProps: { iconName: 'PlugConnected' },
                            name: 'Connect',
                            disabled: environmentIsALie(environment) || isNotAvailable(environment),
                            href: `environment/${environment.id!}`,
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
            <DeleteDialog
                environment={environment}
                deleteEnvironment={deleteEnvironment}
                // tslint:disable-next-line: react-this-binding-issue
                cancel={() => {
                    setDeleteDialogHidden(true);
                }}
                hidden={deleteDialogHidden}
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

export function EnvironmentCard(props: EnvironmentCardProps) {
    const environmentNameText = <Text variant={'large'}>{props.environment.friendlyName}</Text>;
    const environmentName =
        environmentIsALie(props.environment) || props.environment.state !== StateInfo.Available ? (
            <div className='environment-card__environment-name'>{environmentNameText}</div>
        ) : (
            <Link
                className='environment-card__environment-name'
                href={`environment/${props.environment.id}`}
            >
                {environmentNameText}
            </Link>
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
                />
            </Stack>
        </Stack>
    );
}
