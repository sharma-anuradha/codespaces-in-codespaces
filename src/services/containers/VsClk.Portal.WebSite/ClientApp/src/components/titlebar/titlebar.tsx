import React, { Component } from 'react';
import './titlebar.css';
// import './IconExample.scss';

import { Persona, PersonaSize, PersonaPresence } from 'office-ui-fabric-react/lib/Persona';
import { Icon } from 'office-ui-fabric-react/lib/Icon';

export interface TitleBarProps {}

export class TitleBar extends Component<TitleBarProps> {
    render() {

        return (
            <div className='titlebar part'>
                <div className="titlebar__caption" aria-label="Visual Studio logo">
                    <Icon iconName="VisualStudioLogo" className="titlebar__caption-icon" />
                    <div className="titlebar__caption-text">Visual Studio Online</div>
                </div>
                <Persona
                    className="titlebar__main-avatar"
                    size={PersonaSize.size28}
                    presence={PersonaPresence.online}
                    imageUrl="https://avatars0.githubusercontent.com/u/116461?s=460"
                    />
            </ div>
        );
    }
}
