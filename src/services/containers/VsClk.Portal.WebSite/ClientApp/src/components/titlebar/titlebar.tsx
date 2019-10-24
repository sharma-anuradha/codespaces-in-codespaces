import React from 'react';
import { useSelector } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import { withRouter } from 'react-router';

import { Persona, PersonaSize } from 'office-ui-fabric-react/lib/Persona';
import { Separator } from 'office-ui-fabric-react/lib/Separator';

import { ApplicationState } from '../../reducers/rootReducer';
import { PlanSelector } from '../planSelector/plan-selector';

import './titlebar.css';
import { telemetry } from '../../utils/telemetry';

const getDevelopmentEmojiPrefix = () => {
    const isDev = process.env.NODE_ENV === 'development';

    if (!isDev) {
        return null;
    }

    return <span title='<is local stamp>'> 🚧 </span>;
};

const getIsInternalEmojiPrefix = () => {
    const { isInternal } = telemetry;

    if (!isInternal) {
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
    if (isAuthenticated) {
        persona = (
            <Persona
                className='titlebar__main-avatar'
                size={PersonaSize.size28}
                imageUrl={photoUrl}
                title={title}
            />
        );

        planSelector = <PlanSelector className='vsonline-titlebar__plan-selector' {...props} />;
    }

    return (
        <div className='vsonline-titlebar part'>
            <div className='vsonline-titlebar__caption' aria-label='Visual Studio logo'>
                <div className='vsonline-titlebar__logo' />
                <Separator vertical className='vsonline-titlebar__separator' />
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
