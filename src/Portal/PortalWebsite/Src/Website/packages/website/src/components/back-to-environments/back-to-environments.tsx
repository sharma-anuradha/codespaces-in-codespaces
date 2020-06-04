import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { environmentsPath } from '../../routerPaths';
import React from 'react';
import { useTranslation } from 'react-i18next';
import './back-to-environments.css';

export function BackToEnvironmentsLink() {
    const { t: translation } = useTranslation();
    return (
        <Link className='back-to-env' href={environmentsPath}>
            <span className='back-to-env__back-to-wrapper'>
                <span>{translation('backToCodespaces')}</span>
                <span>
                    <Icon iconName='ChevronRight' className='greater-than-button' />
                </span>
            </span>
        </Link>
    );
}
