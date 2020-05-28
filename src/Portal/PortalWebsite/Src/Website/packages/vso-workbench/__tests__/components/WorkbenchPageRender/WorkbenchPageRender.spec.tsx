import React from 'react';
import renderer from 'react-test-renderer';

import { EnvironmentStateInfo } from 'vso-client-core';

import { WorkbenchPageRender } from '../../../src/react-app/components/WorkbenchPage/WorkbenchPageRender';
import { EnvironmentWorkspaceState } from '../../../src/interfaces/EnvironmentWorkspaceState';

describe('WorkbenchPageRender', () => {
    it('should render WorkbenchPageRender environment states', () => {
        let i = 0;
        for (let state in EnvironmentStateInfo) {
            const message = `${i++}`;

            const component = renderer.create(
                <WorkbenchPageRender
                    environmentInfo={null}
                    environmentState={state as EnvironmentStateInfo}
                    message={message}
                    startEnvironment={() => {}}
                    handleAPIError={() => {}}
                    onSignIn={() => {}}
                />
            );

            let tree = component.toJSON();
            expect(tree).toMatchSnapshot();
        }
    });

    it('should render WorkbenchPageRender workbench states', () => {
        let i = 0;
        for (let state in EnvironmentWorkspaceState) {
            const message = `${i++}`;

            const component = renderer.create(
                <WorkbenchPageRender
                    environmentInfo={null}
                    environmentState={state as EnvironmentWorkspaceState}
                    message={message}
                    startEnvironment={() => {}}
                    handleAPIError={() => {}}
                    onSignIn={() => {}}
                />
            );

            let tree = component.toJSON();
            expect(tree).toMatchSnapshot();
        }
    });
});
