import * as React from 'react';

import { IPerformanceBlock } from '../../../interfaces/ICodespacePerformance';
import { IPerformanceProps } from '../../../interfaces/IPerformanceProps';
import { CodespacePerformance } from '../../../utils/performance/CodespacePerformance';

/**
 * Base class that adds some initialization bits over the codespace performance methods.
 */
export class PerformanceComponent<T extends IPerformanceProps, K> extends React.Component<
    T,
    K
> {
    protected performance: CodespacePerformance;

    constructor(props: T, state: K) {
        super(props, state);

        this.performance = props.performance;
    }

    protected newPerformanceGroup = (
        id: number | string,
        groupName?: string
    ): CodespacePerformance => {
        groupName = groupName ?? this.constructor.name;

        this.performance = this.performance.createGroup(groupName, id);
        return this.performance;
    };

    protected measure = async (options: IPerformanceBlock, callback: Function): Promise<void> => {
        return await this.performance.measure(options, callback as any);
    };
}
