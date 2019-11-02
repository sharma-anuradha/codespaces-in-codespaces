import React, { FC } from 'react';

import { Stack, StackItem, Text } from 'office-ui-fabric-react';
import { PortalLayout } from '../portalLayout/portalLayout';
import { EverywhereImage } from '../EverywhereImage/EverywhereImage';
import { unavailableErrorMessage } from '../../actions/serviceUnavailable';

import './ServiceUnavailable.css';

export const ServiceUnavailable: FC = () => {
    return (
        <PortalLayout hideNavigation>
            <Stack
                horizontalAlign='center'
                verticalFill
                verticalAlign='center'
                tokens={{ childrenGap: 'l1' }}
                className='service-unavailable-page'
            >
                <Stack.Item>
                    <Text className='service-unavailable-page__title'>Visual Studio Online</Text>
                </Stack.Item>

                <StackItem>
                    <EverywhereImage />
                </StackItem>

                <Stack.Item className='service-unavailable-page__message'>
                    {unavailableErrorMessage}
                </Stack.Item>
            </Stack>
        </PortalLayout>
    );
};
