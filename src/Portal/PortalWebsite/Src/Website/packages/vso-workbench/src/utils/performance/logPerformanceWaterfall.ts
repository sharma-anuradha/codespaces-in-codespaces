import color from 'ansi-colors';

import { ICodespacePerformanceBlockMeasure } from "../../interfaces/ICodespacePerformanceBlockMeasure";
import { IWaterfallNode } from "../../interfaces/IWaterfallNode";
import { sortByStartTime } from './sortByStartTime';

const logInternal = (
    type: 'log' | 'groupCollapsed',
    name: string,
    startTime: number,
    duration: number,
    rootStartTime: number
) => {
    const connectionTimes = `[${startTime}ms - ${startTime + duration}ms]`;

    console[type](
        [
            `${name}:`,
            color.cyan(`${duration}ms`),
            color.magenta(`+${startTime - rootStartTime}ms`),
            color.gray(connectionTimes),
        ].join(' ')
    );
};

const log = (name: string, startTime: number, duration: number, rootStartTime: number) => {
    logInternal('log', name, startTime, duration, rootStartTime);
};

const group = (name: string, startTime: number, duration: number, rootStartTime: number) => {
    logInternal('groupCollapsed', name, startTime, duration, rootStartTime);
};

const logEmptyNode = (node: IWaterfallNode, rootStartTime: number) => {
    const { name, measure } = node;
    const { startTime, duration } = measure;

    log(name, Math.round(startTime), Math.round(duration), rootStartTime);
};

export const logPerformanceWaterfall = (root: IWaterfallNode, rootStartTime?: number) => {
    const { measure, name } = root;
    const { startTime, duration } = measure;
    const roundStartTime = Math.round(startTime);

    rootStartTime = rootStartTime ?? Math.round(startTime);

    // log the `root` group
    group(name, roundStartTime, Math.round(duration), rootStartTime);

    // log either `group` or `block`
    for (let item of sortByStartTime([...root.groups])) {
        item.groups.length
            ? logPerformanceWaterfall(item, rootStartTime)
            : logEmptyNode(item, rootStartTime);
    }

    // end the `root` group
    console.groupEnd();
};
