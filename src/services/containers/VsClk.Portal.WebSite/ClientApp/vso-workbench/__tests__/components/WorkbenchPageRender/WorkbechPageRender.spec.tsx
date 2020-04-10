import React from 'react';
import renderer from 'react-test-renderer';

import { EnvironmentStateInfo } from 'vso-client-core';

import { WorkbenchPageRender } from '../../../src/react-app/components/WorkbenchPage/WorkbenchPageRender';
import { EnvironmentWorkspaceState } from '../../../src/interfaces/EnvironmentWorkspaceState';

describe('WorkbechPageRender', () => {
    it('should render WorkbechPageRender environment states', () => {
        let i = 0;
        for (let state in EnvironmentStateInfo) {
            const message = `${i++}`;

            const component = renderer.create(
                <WorkbenchPageRender
                  state={state as EnvironmentStateInfo}
                  message={message}
                  startEnvironment={() => {}}
                  handleAPIError={() => {}}
                  onSignIn={() => {}}
                />);
          
            let tree = component.toJSON();
            expect(tree).toMatchSnapshot();
        }
    });

    it('should render WorkbechPageRender workbench states', () => {
        let i = 0;
        for (let state in EnvironmentWorkspaceState) {
            const message = `${i++}`;

            const component = renderer.create(
                <WorkbenchPageRender
                  state={state as EnvironmentWorkspaceState}
                  message={message}
                  startEnvironment={() => {}}
                  handleAPIError={() => {}}
                  onSignIn={() => {}}
                />);
          
            let tree = component.toJSON();
            expect(tree).toMatchSnapshot();
        }
    });
});