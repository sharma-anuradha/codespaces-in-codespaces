import React, { useState, useCallback, MouseEventHandler, useMemo, FC } from 'react';
import classnames from 'classnames';

import { Stack, IStackProps, StackItem } from 'office-ui-fabric-react/lib/Stack';
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';

import './collapsible.css';
import { isDefined } from '../../utils/isDefined';

type Props = {
    title: string;
    collapsed?: boolean;
    onCollapsedChanged?: MouseEventHandler<HTMLButtonElement>;
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
    const toggle: MouseEventHandler<HTMLButtonElement> = useCallback(
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
