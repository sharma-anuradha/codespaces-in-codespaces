import React, { useState, useCallback } from 'react';
import classnames from 'classnames';

import { Stack, IStackProps, StackItem } from 'office-ui-fabric-react/lib/Stack';
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';

import './collapsible.css';

type Props = { title: string } & IStackProps;

export function Collapsible(props: React.PropsWithChildren<Props>) {
    const { title } = props;

    const [collapsed, setCollapsed] = useState(true);

    const toggle = useCallback(() => {
        setCollapsed(!collapsed);
    }, [collapsed, setCollapsed]);

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
                >
                    {title}
                </DefaultButton>
            </StackItem>
            <div
                className={classnames('collapsible__content', {
                    'collapsible__content-collapsed': collapsed,
                })}
            >
                {props.children}
            </div>
        </Stack>
    );
}
