import { Image, Stack, StackItem } from 'office-ui-fabric-react';
import { Icon } from 'office-ui-fabric-react/lib/Icon';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Text } from 'office-ui-fabric-react/lib/Text';
import React from 'react';
import { PortalLayout } from '../portalLayout/portalLayout';
import errorUfo from './error-ufo-404.svg';
import '../portalLayout/portalLayout.css';
import './pageNotFound.css';

export function PageNotFound() {
    return (
        <PortalLayout>
            <Stack
                horizontalAlign='center'
                verticalFill
                verticalAlign='center'
                className='page-not-found'
                tokens={{ childrenGap: '20' }}
            >
                <Stack.Item>
                    <Text className='page-not-found__title'>
                        We can't find what you are looking for.
                    </Text>
                </Stack.Item>

                <Stack.Item className='page-not-found__back-to-wrapper'>
                    <Link className='page-not-found__back-to-wrapper' href={'/environments'}>
                        <span className='page-not-found__back-to'>
                            <span>Back to environments</span>
                            <span>
                                <Icon
                                    iconName='ChevronRight'
                                    className='page-not-found__greater-than-button'
                                />
                            </span>
                        </span>
                    </Link>
                </Stack.Item>

                <StackItem>
                    <Image src={errorUfo} />
                </StackItem>
            </Stack>
        </PortalLayout>
    );
}
