import React from 'react';
import { useSelector } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import { withRouter } from 'react-router';

import { Persona, PersonaSize } from 'office-ui-fabric-react/lib/Persona';
import { Separator } from 'office-ui-fabric-react/lib/Separator';

import { ApplicationState } from '../../reducers/rootReducer';
import { AccountSelector } from '../accountSelector/accountSelector';

import './titlebar.css';

function TitleBarNoRouter(props: RouteComponentProps) {
    const userInfo = useSelector((state: ApplicationState) => state.userInfo);

    const title = userInfo ? `${userInfo.displayName} <${userInfo.mail}>` : '<unknown>';

    const photoUrl = userInfo ? userInfo.photoUrl : undefined;

    const pageTitle =
        process.env.NODE_ENV === 'development' ? '🚧 Visual Studio Online' : 'Visual Studio Online';

    return (
        <div className='vsonline-titlebar part'>
            <div className='vsonline-titlebar__caption' aria-label='Visual Studio logo'>
                <div className='vsonline-titlebar__logo' />
                <Separator vertical className='vsonline-titlebar__separator' />
                <div className='vsonline-titlebar__caption-text'>{pageTitle}</div>
                <Separator vertical className='vsonline-titlebar__separator' />
                <AccountSelector {...props} />
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
