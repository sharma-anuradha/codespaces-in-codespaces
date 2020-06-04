import React from 'react';

import { DefaultButton, IButtonProps } from 'office-ui-fabric-react/lib/Button';

import { EverywhereImage } from '../EverywhereImage/EverywhereImage';
import { useTranslation } from 'react-i18next';

interface NoEnvironmentProps {
    onClick: IButtonProps['onClick'];
}

export const NoEnvironments = ({ onClick }: NoEnvironmentProps) => {
    const { t: translation } = useTranslation();
    return (
        <div className='environments-panel__no-environments' key='no-envs'>
            <span className='environments-panel__no-environments-label'>
                {translation('noCodespaces')}
            </span>

            <EverywhereImage className='environments-panel__no-environments-image' />

            <DefaultButton onClick={onClick} primary={true}>
                {translation('createCodespace')}
            </DefaultButton>
        </div>
    );
};
