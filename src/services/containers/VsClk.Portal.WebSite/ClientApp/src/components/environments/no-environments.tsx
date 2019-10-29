import React from 'react';

import { DefaultButton, IButtonProps } from 'office-ui-fabric-react/lib/Button';
import { Image } from 'office-ui-fabric-react/lib/Image';

import image from '../login/login-image.png';

interface NoEnvironmentProps {
    onClick: IButtonProps['onClick'];
}

export const NoEnvironments = ({ onClick }: NoEnvironmentProps) => {
    return (
        <div className='environments-panel__no-environments' key='no-envs'>
            <span className='environments-panel__no-environments-label'>
                You don't have any environments
            </span>
            
            <Image className='environments-panel__no-environments-image' src={image} width={326} height={193} />

            <DefaultButton onClick={onClick} primary={true}>
                Create environment
            </DefaultButton>
        </div>
    );
};
