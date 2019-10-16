import React from 'react';
import { useSelector } from 'react-redux';

import { Persona, PersonaSize } from 'office-ui-fabric-react/lib/Persona';
import { Separator } from 'office-ui-fabric-react/lib/Separator';

import { ApplicationState } from '../../reducers/rootReducer';

import './titlebar.css';

export function TitleBar() {
    const userInfo = useSelector((state: ApplicationState) => state.userInfo);


    const title = (userInfo)
        ? `${userInfo.displayName} <${userInfo.mail}>`
        : '<unknown>';
    
    const photoUrl = (userInfo)
        ? userInfo.photoUrl
        : undefined;

    return (
        <div className='vsonline-titlebar part'>
            <div className='vsonline-titlebar__caption' aria-label='Visual Studio logo'>
                <div className='vsonline-titlebar__logo' />
                <Separator vertical className='vsonline-titlebar__separator' />
                <div className='vsonline-titlebar__caption-text'>Visual Studio Online</div>
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
