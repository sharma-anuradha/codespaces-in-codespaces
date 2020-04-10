import * as React from 'react';

export interface IButtonLinkProps {
    text: string;
    url?: string;
    onClick?: () => any;
    className?: string;
}

import './ButtonLink.css';

export const ButtonLink: React.FunctionComponent<IButtonLinkProps> = (props: IButtonLinkProps) => {
    const { className = '', text, url, onClick } = props;

    if (!url && !onClick) {
        throw new Error('Either `url` or `onClick` properties should be set.');
    }

    const classNameAttr = `vso-button ${className}`;

    if (url) {
        return (
            <a className={classNameAttr} href={url}>
                {text}
            </a>
        );
    }

    return (
        <span className={classNameAttr} onClick={onClick}>
            {text}
        </span>
    );
};
