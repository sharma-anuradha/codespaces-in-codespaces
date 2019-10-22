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
    const isDev = (process.env.NODE_ENV === 'development');

    if (!isDev) {
        return null;
    }

    return (
        <span title='<is local stamp>'> 🚧 </span>
    );    
}

const getIsInternalEmojiPrefix = () => {
    const isInternal = (telemetry.isInternal);

    if (!isInternal) {
        return null;
    }

    return (
        <span title='<is internal user>'> ⭐ </span>
    );    
}

function TitleBarNoRouter(props: RouteComponentProps) {
    const userInfo = useSelector((state: ApplicationState) => state.userInfo);

    const title = userInfo
        ? `${userInfo.displayName} <${userInfo.mail}>`
        : '<unknown>';

    const photoUrl = userInfo
        ? userInfo.photoUrl
        : undefined;

    return (
        <div className='vsonline-titlebar part'>
            <div className='vsonline-titlebar__caption' aria-label='Visual Studio logo'>
                <div className='vsonline-titlebar__logo' />
                <Separator vertical className='vsonline-titlebar__separator' />
                <div className='vsonline-titlebar__caption-text'>
                    { getDevelopmentEmojiPrefix() }
                    { getIsInternalEmojiPrefix() }
                    &nbsp;Visual Studio Online
                </div>
                <PlanSelector className='vsonline-titlebar__plan-selector' {...props} />
            </div>
            <Persona
                className='titlebar__main-avatar'
                size={PersonaSize.size28}
                imageUrl={photoUrl}
                title={title}
            />
        </div>
    );
}

export const TitleBar = withRouter(TitleBarNoRouter);
