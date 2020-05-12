import React, { Component } from 'react';

import { Link } from 'office-ui-fabric-react/lib/Link';
import { Icon } from 'office-ui-fabric-react/lib/Icon';

import './icon-label.css';

export interface IconLabelProps {
    className?: string;
    label: string;
    iconName: string;
}

export class IconLabel extends Component<IconLabelProps> {
    render() {
        const { className = '', label, iconName } = this.props;

        return (
            <div
                className={`icon-label ${className}`}
                aria-label={label}
                title={label}>
                    <Icon className='icon-label__icon' iconName={iconName} />
                    <Link className='icon-label__label'>{ label }</Link>
            </div>
        );
    }
}
