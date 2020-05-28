import React from 'react';

export const CodeLine: React.FunctionComponent<{
    percentage: number;
}> = (props) => {
    const style = {
        width: `${props.percentage}%`
    };
    return (<li className='line' style={style}></li>);
};
