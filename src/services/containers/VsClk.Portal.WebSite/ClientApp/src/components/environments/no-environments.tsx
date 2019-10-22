import React from 'react';

import { DefaultButton, IButtonProps } from 'office-ui-fabric-react/lib/Button';

interface NoEnvironmentProps {
    onClick: IButtonProps['onClick'];
}

export const NoEnvironmnets = ({ onClick }: NoEnvironmentProps) => {
    return (
        <div className='environments-panel__no-environments' key='no-envs'>
            <span className='environments-panel__no-environments-label'>
                You don't have any environments
            </span>
            <DefaultButton
                onClick={onClick}
                primary={true}
            >
                Create environment
            </DefaultButton>
        </div>
    );
}
