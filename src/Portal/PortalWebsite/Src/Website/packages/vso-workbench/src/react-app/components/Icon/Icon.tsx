import * as React from 'react';

import './icon.css';

type TIconType = 'tick' | 'error' | 'spinner';
type TIconColor = 'red' | 'green' | 'blue' | 'black';

export interface IIconProps {
    type: TIconType;
    color?: TIconColor;
    className?: string;
}

const getIconColorClassName = (color: TIconColor) => {
    switch (color) {
        case 'red':
        case 'green':
        case 'blue':
        case 'black': {
            return `vscs-icon--${color}`;
        }

        default: {
            throw new Error(`Uknown icon color "${color}".`);
        }
    }
};

export const Icon: React.FunctionComponent<IIconProps> = (props: IIconProps) => {
    const { type, className, color = 'black' } = props;

    return (
        <span className={`vscs-icon ${getIconColorClassName(color)} ${className || ''}`}>
            <svg
                viewBox='0 0 16 16'
                xmlns='http://www.w3.org/2000/svg'
                xmlnsXlink='http://www.w3.org/1999/xlink'
            >
                <use xlinkHref={`#${type}-vscs-icon`} />
            </svg>
        </span>
    );
};
