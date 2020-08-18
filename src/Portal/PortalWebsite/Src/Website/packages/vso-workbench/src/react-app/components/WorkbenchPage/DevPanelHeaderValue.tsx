import * as React from 'react';

import './DevPanelHeaderValue.css';

interface IDevPanelHeaderValueProps {
    text?: string;
}

type TDevPanelHeaderValueProps = React.PropsWithChildren<IDevPanelHeaderValueProps>;

export const DevPanelHeaderValue: React.FunctionComponent<TDevPanelHeaderValueProps> = (
    props: TDevPanelHeaderValueProps
) => {
    const { text = '', children } = props;
    const selectrionDividerStyle = {
        opacity: 0,
        fontSize: 0,
    };

    return (
        <>
            <i style={selectrionDividerStyle}>{'{'}</i>
            <span className='vscs-dev-panel-header__section-value'>
                {text}
                {children}
            </span>
            <i style={selectrionDividerStyle}>{'}'}</i>
        </>
    );
};
