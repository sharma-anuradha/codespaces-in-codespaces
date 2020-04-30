import React from 'react';

import { DefaultButton, IButtonProps } from 'office-ui-fabric-react/lib/Button';

import { EverywhereImage } from '../EverywhereImage/EverywhereImage';

interface NoEnvironmentProps {
    onClick: IButtonProps['onClick'];
}

export const NoEnvironments = ({ onClick }: NoEnvironmentProps) => {
    return (
        <div className='environments-panel__no-environments' key='no-envs'>
            <span className='environments-panel__no-environments-label'>
                You don't have any Codespaces
            </span>

            <EverywhereImage className='environments-panel__no-environments-image' />

            <DefaultButton onClick={onClick} primary={true}>
                Create Codespace
            </DefaultButton>
        </div>
    );
};
