import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { environmentsPath } from '../../routerPaths';
import React from 'react';
import './back-to-environments.css';

export function BackToEnvironmentsLink() {
    return (
        <Link className='back-to-env' href={environmentsPath}>
            <span className='back-to-env__back-to-wrapper'>
                <span>Back to environments </span>
                <span>
                    <Icon iconName='ChevronRight' className='greater-than-button' />
                </span>
            </span>
        </Link>
    );
}
