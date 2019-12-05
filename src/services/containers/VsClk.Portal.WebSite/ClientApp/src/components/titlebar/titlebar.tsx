import React, { Component } from 'react';
import { useSelector } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import { withRouter } from 'react-router';

import { Persona, PersonaSize } from 'office-ui-fabric-react/lib/Persona';
import {
    HoverCard,
    HoverCardType,
    IPlainCardProps,
    IHoverCard,
} from 'office-ui-fabric-react/lib/HoverCard';
import { Toggle } from 'office-ui-fabric-react/lib/Toggle';
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { KeyCodes } from 'office-ui-fabric-react/lib/Utilities';
import { Panel } from 'office-ui-fabric-react/lib/Panel';

import { ApplicationState } from '../../reducers/rootReducer';
import { PlanSelector } from '../planSelector/plan-selector';
import { logout } from '../../actions/logout';

import './titlebar.css';
import { isInternalUser } from '../../services/isInternalUserTracker';
import { telemetry } from '../../utils/telemetry';

const getDevelopmentEmojiPrefix = () => {
    const isDev = process.env.NODE_ENV === 'development';

    if (!isDev) {
        return null;
    }

    return <span title='<is local stamp>'> 🚧 </span>;
};

const getIsInternalEmojiPrefix = () => {
    if (!isInternalUser) {
        return null;
    }

    return <span title='<is internal user>'> ⭐ </span>;
};

interface ISettingsMenuState {
    open: boolean;
}

interface ISettingsMenuProps {
    hoverCardRef: React.RefObject<IHoverCard>;
}

class SettingsMenu extends Component<ISettingsMenuProps, ISettingsMenuState> {
    public constructor(props: ISettingsMenuProps) {
        super(props);
        this.state = {
            open: false,
        };
    }
    render() {
        return (
            <div className='vsonline-avatarmenu__item'>
                <DefaultButton
                    className='vsonline-avatarmenu__item-button'
                    iconProps={{ iconName: 'Settings' }}
                    onClick={() => this.setState({ open: true })}
                >
                    Settings
                </DefaultButton>
                <Panel
                    headerText='Settings'
                    isOpen={this.state.open}
                    onDismiss={() => {
                        this.setState({ open: false });
                        if (this.props.hoverCardRef.current) {
                            this.props.hoverCardRef.current.dismiss();
                        }
                    }}
                    closeButtonAriaLabel='Close'
                >
                    <Toggle
                        label='Insiders channel'
                        defaultChecked={window.localStorage.getItem('vso-featureset') === 'insider'}
                        onText='On'
                        offText='Off'
                        onChange={(e, checked) => {
                            window.localStorage.setItem(
                                'vso-featureset',
                                checked ? 'insider' : 'stable'
                            );
                            telemetry.setVscodeConfig();
                        }}
                    ></Toggle>
                </Panel>
            </div>
        );
    }
}

function TitleBarNoRouter(props: RouteComponentProps) {
    const { userInfo, isAuthenticated } = useSelector(
        ({ userInfo, authentication: { isAuthenticated } }: ApplicationState) => ({
            userInfo,
            isAuthenticated,
        })
    );

    let title = '<unknown>';
    let photoUrl = undefined;
    if (userInfo) {
        title = `${userInfo.displayName} <${userInfo.mail}>`;

        if (userInfo.photoUrl) {
            photoUrl = userInfo.photoUrl;
        }
    }

    let planSelector = null;
    let persona = null;

    let hoverCardRef = React.createRef<IHoverCard>();

    if (isAuthenticated) {
        const plainCardProps: IPlainCardProps = {
            onRenderPlainCard: () => (
                <div className='vsonline-avatarmenu'>
                    <SettingsMenu hoverCardRef={hoverCardRef} />
                    <div className='vsonline-avatarmenu__item'>
                        <DefaultButton
                            className='vsonline-avatarmenu__item-button'
                            onClick={logout}
                        >
                            Sign out
                        </DefaultButton>
                    </div>
                </div>
            ),
        };

        persona = (
            <HoverCard
                type={HoverCardType.plain}
                cardOpenDelay={60 * 1000}
                sticky={true}
                plainCardProps={plainCardProps}
                openHotKey={KeyCodes.enter}
                trapFocus={true}
                instantOpenOnClick={true}
                componentRef={hoverCardRef}
            >
                <button className='vsonline-titlebar-persona-button'>
                    <Persona
                        className='titlebar__main-avatar'
                        size={PersonaSize.size28}
                        imageUrl={photoUrl}
                        title={title}
                    />
                </button>
            </HoverCard>
        );

        planSelector = <PlanSelector className='vsonline-titlebar__plan-selector' {...props} />;
    }

    return (
        <div className='vsonline-titlebar part'>
            <div className='vsonline-titlebar__caption' aria-label='Visual Studio logo'>
                <div className='vsonline-titlebar__logo' />
                <div className='vsonline-titlebar__separator' />
                <div className='vsonline-titlebar__caption-text'>
                    {getDevelopmentEmojiPrefix()}
                    {getIsInternalEmojiPrefix()}
                    &nbsp;Visual Studio Online
                </div>
                {planSelector}
            </div>
            {persona}
        </div>
    );
}

export const TitleBar = withRouter(TitleBarNoRouter);
