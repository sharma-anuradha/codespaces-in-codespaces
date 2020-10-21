import * as React from 'react';
import { IPerformanceProps } from '../../../interfaces/IPerformanceProps';

import {
    CodespacePerformance,
    getMainCodespacePerformance,
} from '../../../utils/performance/CodespacePerformance';
import { PerformanceEventIds } from '../../../utils/performance/PerformanceEvents';
import { seconds } from '../../../utils/seconds';
import { connect } from '../utils/connect';

import { PerformanceSection, PerformanceBlock } from './DevPanelPerformanceBlock';

const DevPanelHeaderPerformanceComponent: React.FunctionComponent<IPerformanceProps> = (
    props: IPerformanceProps
) => {
    const startBlock = props.performance.getBlock(PerformanceEventIds.Start);
    if (!startBlock) {
        throw new Error('No `start` performance block found.');
    }

    const { measure } = startBlock;
    const { startTime } = measure;

    return (
        <span className='vscs-dev-panel-header__section'>
            <PerformanceSection
                title={`time to javascript - ${seconds(startTime)}`}
                text={`🎨 ${seconds(startTime)}`}
            />
            <PerformanceBlock
                emoji='🔧'
                title='vscode server startup time'
                groupId={PerformanceEventIds.VSCodeServerStartup}
            />
            <PerformanceBlock
                emoji='📡'
                title='workbench initialization (including connection)'
                startEvent={{ id: PerformanceEventIds.OpenSshChannel, type: 'start' }}
                endEvent={{ id: PerformanceEventIds.FinishingConnection, type: 'end' }}
                groupId={PerformanceEventIds.WorkbenchComponent}
            />
            <PerformanceBlock
                emoji='🧩'
                title='time to terminal/extensions'
                startEvent={{ id: PerformanceEventIds.OpenSshChannel, type: 'start' }}
                endEvent={{ id: PerformanceEventIds.InitTimeToRemoteExtensions, type: 'end' }}
                groupId={PerformanceEventIds.Start}
            />
        </span>
    );
};

type TConnectedProps = Omit<IPerformanceProps, 'performance'>;
export const DevPanelHeaderPerformance = connect(
    (props: TConnectedProps): IPerformanceProps => {
        return {
            ...props,
            performance: getMainCodespacePerformance(),
        };
    },
    DevPanelHeaderPerformanceComponent
);
