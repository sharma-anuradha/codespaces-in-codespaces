import React from 'react';
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
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { KeyCodes } from 'office-ui-fabric-react/lib/Utilities';

import { ApplicationState } from '../../reducers/rootReducer';
import { PlanSelector } from '../planSelector/plan-selector';
import { logout } from '../../actions/logout';

import './titlebar.css';
import { isInternalUser } from '../../services/isInternalUserTracker';
import { settingsPath } from '../../routerPaths';

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
                    <div className='vsonline-avatarmenu__item'>
                        <DefaultButton
                            className='vsonline-avatarmenu__item-button'
                            iconProps={{ iconName: 'Settings' }}
                            onClick={() => {
                                props.history.push(settingsPath);
                            }}
                        >
                            Settings
                        </DefaultButton>
                    </div>
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
