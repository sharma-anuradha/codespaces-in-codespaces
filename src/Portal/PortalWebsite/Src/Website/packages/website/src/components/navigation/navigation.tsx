import React from 'react';
import { ActionButton } from 'office-ui-fabric-react/lib/Button';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { NavLink } from 'react-router-dom';

import { environmentsPath, settingsPath } from '../../routerPaths';

import './navigation.css';

type NIProps = { text?: string; iconName: string; to: string };
function NavigationItem({ text, iconName, to }: NIProps) {
    return (
        <NavLink to={to} activeClassName="navigation__row-active">
            <ActionButton iconProps={{ iconName, className: 'navigation__link-icon' }}>
                {text}
            </ActionButton>
        </NavLink>
    );
}

export function Navigation() {
    const navItems = [
        { iconName: 'FrontCamera', text: 'Codespaces', to: environmentsPath, selected: true },
        { iconName: 'Settings', text: 'Settings', to: settingsPath, selected: true },
    ];

    const toElement = (e: NIProps, index: number) => {
        return <NavigationItem {...e} key={index} />;
    };

    return (
        <nav className='navigation'>
            <Stack style={{ flexGrow: 1 }}>{navItems.map(toElement)}</Stack>
        </nav>
    );
}
