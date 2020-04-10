import React from 'react';

import { Image, Stack, StackItem } from 'office-ui-fabric-react';
import { Text } from 'office-ui-fabric-react/lib/Text';
import { isHostedOnGithub } from 'vso-client-core';

import { PortalLayout } from '../portalLayout/portalLayout';
import { BackToEnvironmentsLink } from '../back-to-environments/back-to-environments';
import '../portalLayout/portalLayout.css';
import errorUfo from './error-ufo-404.svg';
import './pageNotFound.css';


export function PageNotFound() {
    return (
        <PortalLayout hideNavigation={isHostedOnGithub()}>
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
                    <BackToEnvironmentsLink />
                </Stack.Item>

                <StackItem>
                    <Image src={errorUfo} />
                </StackItem>
            </Stack>
        </PortalLayout>
    );
}
