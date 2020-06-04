import React from 'react';
import { DefaultButton, IButtonProps } from 'office-ui-fabric-react/lib/Button';
import { useTranslation } from 'react-i18next';

interface NoPlansProps {
    onClick: IButtonProps['onClick'];
}

export const NoPlans = ({ onClick }: NoPlansProps) => {
    const { t: translation } = useTranslation();
    return (
        <div className='environments-panel__no-environments' key='no-plans'>
            <span className='environments-panel__no-environments-label'>
                {translation('noPlans')}
            </span>
            <DefaultButton
                onClick={onClick}
                primary={true}>
            {translation('createNewPlan')}
            </DefaultButton>
        </div>
    );
}
