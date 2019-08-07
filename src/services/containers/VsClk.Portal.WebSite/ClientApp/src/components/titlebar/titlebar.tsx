import React, { Component } from 'react';
import './titlebar.css';

import { Persona, PersonaSize, PersonaPresence } from 'office-ui-fabric-react/lib/Persona';
import { Separator } from 'office-ui-fabric-react/lib/Separator';

export interface TitleBarProps {}

export class TitleBar extends Component<TitleBarProps> {
    render() {
        return (
            <div className='titlebar part'>
                <div className='titlebar__caption' aria-label='Visual Studio logo'>
                    <div className='titlebar__logo' />
                    <Separator vertical className='titlebar__separator' />
                    <div className='titlebar__caption-text'>Visual Studio Online</div>
                </div>
                <Persona
                    className='titlebar__main-avatar'
                    size={PersonaSize.size28}
                    presence={PersonaPresence.online}
                    imageUrl='https://avatars0.githubusercontent.com/u/116461?s=460'
                />
            </div>
        );
    }
}
