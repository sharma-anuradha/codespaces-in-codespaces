import React from 'react';
import { ActionButton } from 'office-ui-fabric-react/lib/Button';
import { Stack } from 'office-ui-fabric-react/lib/Stack';

import './navigation.css';

type NIProps = { text?: string; iconName: string; to?: string };
function NavigationItem({ text, iconName, to }: NIProps) {
    return (
        <ActionButton href={to} iconProps={{ iconName, className: 'navigation__link-icon' }}>
            {text}
        </ActionButton>
    );
}

export function Navigation() {
    const navItems = [
        { iconName: 'FrontCamera', text: 'Codespaces', to: '/environments', selected: true },
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
