import * as React from 'react';
import { FunctionComponent } from 'react';

import { IPerformanceBlockId } from '../../../interfaces/ICodespacePerformance';
import { IWaterfallNode } from '../../../interfaces/IWaterfallNode';
import {
    CodespacePerformance,
    getMainCodespacePerformance,
} from '../../../utils/performance/CodespacePerformance';
import { logPerformanceWaterfall } from '../../../utils/performance/logPerformanceWaterfall';
import { PerformanceEventIds } from '../../../utils/performance/PerformanceEvents';
import { seconds } from '../../../utils/seconds';
import { connect } from '../utils/connect';

interface IPerformanceSectionProps {
    title: string;
    text: string;
    className?: string;
}

/**
 * TODO:
 *  1. Redefine "start"/"end" events and blocks.
 *  2. Add copy button?
 */
export const PerformanceSection: FunctionComponent<IPerformanceSectionProps> = (props) => {
    const { title, text, children, className = '' } = props;

    const classString = `vscs-dev-panel-header__section vscs-dev-panel-header__emoji ${className}`;
    return (
        <div className={classString} title={title}>
            <div style={{ float: 'left' }}>{text}</div>
            {children}
        </div>
    );
};

import './DevPanelPerformanceBlock.css';

export interface IPerformanceBlockProps {
    emoji: string;
    title: string;
    // if start/end events not specified,
    // we take those from the `groupId` block
    startEvent?: IPerformanceBlockId;
    endEvent?: IPerformanceBlockId;
    groupId: PerformanceEventIds;
    performance: CodespacePerformance;
    // whether blocks is show by default or
    // after the "start" event fired. default: false
    isShownByDefault?: boolean;
}

interface IPerformanceBlockState {
    isStarted: boolean;
    timingBlock?: IWaterfallNode;
}


const DEFAULT_STATE: IPerformanceBlockState = {
    isStarted: false,
};

class PerformanceBlockComponent extends React.Component<
    IPerformanceBlockProps,
    IPerformanceBlockState
> {
    constructor(props: IPerformanceBlockProps, public readonly state: IPerformanceBlockState) {
        super(props);

        this.state = {
            ...DEFAULT_STATE,
            isStarted: !!props.isShownByDefault,
        };
    }

    public componentDidMount() {
        const { startEvent, endEvent, groupId, performance } = this.props;

        const start = startEvent ?? { type: 'start', id: groupId };
        performance.onEvent(start, () => {
            this.setState({ isStarted: true });
        });

        const end = endEvent ?? { type: 'end', id: groupId };
        performance.onEvent(end, () => {
            const waterfall = performance.getBlock(groupId);
            if (!waterfall) {
                throw new Error(`The "${groupId}" group not found.`);
            }
            this.setState({ timingBlock: waterfall });
        });
    }

    private getText = () => {
        const { emoji } = this.props;
        const { timingBlock } = this.state;

        if (!timingBlock) {
            return `⌛`;
        }

        const { measure } = timingBlock;
        return `${emoji} ${seconds(measure.duration)}`;
    };

    private getTitle = () => {
        const { title } = this.props;
        const { timingBlock } = this.state;

        if (!timingBlock) {
            return title;
        }

        const { measure } = timingBlock;
        const { startTime, duration } = measure;

        return `${title} [${Math.round(startTime)} - ${Math.round(startTime + duration)}]`;
    };

    private logWaterfall = () => {
        const { groupId, performance } = this.props;
        const waterfall = performance.getBlock(groupId);

        if (!waterfall) {
            console.error(`No "${groupId}" waterfall found.`);
            return;
        }

        logPerformanceWaterfall(waterfall);
    };

    public render() {
        const { isStarted, timingBlock } = this.state;

        if (!isStarted) {
            return null;
        }

        const classString = timingBlock ? 'is-ready' : '';

        return (
            <PerformanceSection
                className={`vscs-dev-panel-header-perf-block ${classString}`}
                title={this.getTitle()}
                text={this.getText()}
            >
                <div className='vscs-dev-panel-header-perf-block__controls'>
                    <button
                        onClick={this.logWaterfall}
                        className='vso-button vscs-dev-panel__input vscs-dev-panel__input--button vscs-dev-panel-header-perf-block__console-button'
                        title='log time breakdown to console'
                    >
                        ..
                    </button>
                </div>
            </PerformanceSection>
        );
    }
}

type TPerformanceBlockProps = Omit<IPerformanceBlockProps, 'performance'>;
export const PerformanceBlock = connect((props: TPerformanceBlockProps): IPerformanceBlockProps => {
    return {
        ...props,
        performance: getMainCodespacePerformance(),
    };
}, PerformanceBlockComponent);
