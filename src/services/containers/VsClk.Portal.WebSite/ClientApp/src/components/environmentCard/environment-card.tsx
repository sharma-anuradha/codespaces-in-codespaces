import React, { Component } from 'react';

import { Link } from 'office-ui-fabric-react/lib/Link';
import { Separator } from 'office-ui-fabric-react/lib/Separator';
import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { Persona, PersonaSize, PersonaPresence } from 'office-ui-fabric-react/lib/Persona';
import { users } from '../mocks/users';
import { environments as envsMock } from '../mocks/environments';

import { Depths } from '@uifabric/fluent-theme/lib/fluent/FluentDepths';
import './environment-card.css';
import { SharedColors, NeutralColors, CommunicationColors } from '@uifabric/fluent-theme/lib/fluent/FluentColors';


import { IconLabel } from '../iconLabel/icon-label';
import { ICloudEnvironment } from '../../interfaces/cloudenvironment';

import { Link as RouterLink } from 'react-router-dom';

export interface EnvironmentCardProps {
    className?: string;
    environment: ICloudEnvironment;
    id: number;
}

export class EnvironmentCard extends Component<EnvironmentCardProps> {

    private getUsers() {
        const { id } = this.props;

        switch (id) {
            case 1: {
                return [users[0], users[1], users[2]];
            }
            case 2: {
                return [users[3], users[4], users[5]];
            }
            case 3:
            default: {
                return [users[6], users[0], users[3]];
            }
        }
    }

    private renderUsers() {
        const users = this.getUsers();

        const usersElements = [];
        let i = 0;
        for (let user of users) {
            usersElements.push(
                <Persona
                    key={i++}
                    className='environment-card__user'
                    size={PersonaSize.size24}
                    presence={user.presence}
                    imageUrl={user.imageUrl}
                />
            );
        }

        return (
            <div className='ms-Grid-row environment-card__users'>
                {usersElements}
            </div>
        )
    }

    private renderFooter() {
        return (
            <div className='ms-Grid' dir='ltr'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm6 ms-md4 ms-lg1 environment-card__icon'>
                        <Icon
                            iconName='OpenInNewWindow'
                            style={{ color: CommunicationColors.primary }}
                        />
                    </div>
                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg1 environment-card__icon'>
                        <Icon
                            iconName='GitGraph'
                            style={{ color: CommunicationColors.primary }}
                        />
                    </div>
                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg10'></div>
                </div>
            </div>
        );
    }
    
    render() {
        const { className = '', environment, id } = this.props;

        const envMock = envsMock[id - 1];

        return (
            <RouterLink
                to={`/environment/${environment.id}`}
                className={`environment-card ${className}`}
                style={{ boxShadow: Depths.depth8 }}>
                <div className='ms-Grid' dir='ltr'>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm6 ms-md4 ms-lg9'>
                            <IconLabel
                                label={environment.friendlyName}
                                iconName='VisualStudioLogo' />
                        </div>
                        <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg3'>
                            <Link
                                style={{ backgroundColor: SharedColors.greenCyan10, color: NeutralColors.gray10 }}
                                className='environment-card__tag'>
                                    Online
                            </Link>
                        </div>
                    </div>
                    {/* <div className='ms-Grid-row environment-card__environment-description'>
                        { envMock.description }
                    </div> */}
                    <div className='ms-Grid-row environment-card__date'>
                        { envMock.date }
                    </div>
                    {this.renderUsers()}
                </div>
                <Separator />
                {this.renderFooter()}
            </RouterLink>
        );
    }
}
