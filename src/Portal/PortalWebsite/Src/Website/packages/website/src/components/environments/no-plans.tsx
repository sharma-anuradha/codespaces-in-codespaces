import React from 'react';
import { DefaultButton, IButtonProps } from 'office-ui-fabric-react/lib/Button';

interface NoPlansProps {
    onClick: IButtonProps['onClick'];
}

export const NoPlans = ({ onClick }: NoPlansProps) => {
    return (
        <div className='environments-panel__no-environments' key='no-plans'>
            <span className='environments-panel__no-environments-label'>
                You don't have any plans yet.
            </span>
            <DefaultButton
                onClick={onClick}
                primary={true}>
            Create new plan
            </DefaultButton>
        </div>
    );
}
