import { isDefined } from 'vso-client-core';

import React, { useState, useCallback, MouseEventHandler, FC } from 'react';
import classnames from 'classnames';

import { Stack, IStackProps, StackItem } from 'office-ui-fabric-react/lib/Stack';
import { DefaultButton, BaseButton, Button } from 'office-ui-fabric-react/lib/Button';

import './collapsible.css';

type Props = {
    title: string;
    collapsed?: boolean;
    onCollapsedChanged?: MouseEventHandler<
        | HTMLDivElement
        | HTMLAnchorElement
        | HTMLButtonElement
        | BaseButton
        | Button
        | HTMLSpanElement
    >;
} & IStackProps;

export const Collapsible: FC<Props> = ({
    title,
    collapsed: controlledCollapsed,
    onCollapsedChanged,
    children,
    ...stackProps
}) => {
    const [internalCollapsed, setInternalCollapsed] = useState(true);

    const collapsed = isDefined(controlledCollapsed) ? controlledCollapsed : internalCollapsed;
    const toggle: MouseEventHandler<
        | HTMLDivElement
        | HTMLAnchorElement
        | HTMLButtonElement
        | BaseButton
        | Button
        | HTMLSpanElement
    > = useCallback(
        (event) => {
            if (onCollapsedChanged) {
                onCollapsedChanged(event);
            }

            if (!event.isDefaultPrevented() && !isDefined(controlledCollapsed)) {
                setInternalCollapsed(!internalCollapsed);
            }
        },
        [internalCollapsed, controlledCollapsed, onCollapsedChanged]
    );

    const collapsibleName = `${collapsed ? 'Collapsed' : 'Uncollapsed'}, ${title}`;
    return (
        <Stack>
            <StackItem>
                <DefaultButton
                    toggle
                    iconProps={{
                        iconName: 'ChevronDown',
                        style: {
                            transform: collapsed ? 'rotate(-90deg)' : undefined,
                            transition: 'transform 0.1s linear 0s',
                        },
                    }}
                    style={{
                        border: 'none',
                        background: 'none',
                        padding: 0,
                    }}
                    className='collapsible__button'
                    onClick={toggle}
                    name={collapsibleName}
                    ariaLabel={collapsibleName}
                >
                    {title}
                </DefaultButton>
            </StackItem>
            <StackItem
                className={classnames('collapsible__content', {
                    'collapsible__content-collapsed': collapsed,
                })}
            >
                <Stack {...stackProps}>{children}</Stack>
            </StackItem>
        </Stack>
    );
};
