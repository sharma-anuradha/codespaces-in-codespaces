import { Container } from "Fx/Composition/TemplateBlade";
import * as CustomHtml from "Fx/Controls/CustomHtml";
import * as MonitorChartV2 from "Fx/Controls/MonitorChartV2";
import * as OptionsGroup from "Fx/Controls/OptionsGroup";
import * as Section from "Fx/Controls/Section";

import * as ClientResources from "ClientResources";

export import Metric = MonitorChartV2.Metric;
export import FilterCollection = MonitorChartV2.FilterCollection;
export import Grouping = MonitorChartV2.Grouping;

export interface ChartConfig {
    readonly title: string;
    readonly metrics: Metric.Options[];
    readonly filterCollection: FilterCollection;
    readonly grouping: Grouping;
}

export interface Options {
    readonly chartConfig: ChartConfig;
}

export interface Contract {
    readonly control: CustomHtml.Contract;
    refresh(): void;
}

export function create(container: Container, options: Options): Contract {
    return new MonitoringComponent(container, options);
}

const HtmlTemplate = `
<div class="ext-monitoring-component">
    <div class="ext-monitoring-component__timespan" data-bind="pcControl: timeSelector"></div>
    <div class="ext-monitoring-component__chart" data-bind="pcControl: monitorChart"></div>
</div>
`;

const enum TimespansMs {
    oneHour = 1 * 60 * 60 * 1000,
    sixHours = oneHour * 6,
    twelveHours = oneHour * 12,
    oneDay = oneHour * 24,
    sevenDays = oneDay * 7,
    thirtyDays = oneDay * 30,
}

const MonitoringStrings = ClientResources.Monitoring;

class MonitoringComponent {
    public readonly control: CustomHtml.Contract;

    private readonly _monitorChart: MonitorChartV2.Contract;

    constructor(container: Container, options: Options) {
        // Time selector
        const timeSelector = OptionsGroup.create(container, {
            label: MonitoringStrings.showDataForLast,
            suppressDirtyBehavior: true,
            uniformItemWidth: true,
            items: [
                {
                    text: MonitoringStrings.oneHour,
                    value: TimespansMs.oneHour,
                },
                {
                    text: MonitoringStrings.sixHours,
                    value: TimespansMs.sixHours,
                },
                {
                    text: MonitoringStrings.twelveHours,
                    value: TimespansMs.twelveHours,
                },
                {
                    text: MonitoringStrings.oneDay,
                    value: TimespansMs.oneDay,
                },
                {
                    text: MonitoringStrings.sevenDays,
                    value: TimespansMs.sevenDays,
                },
                {
                    text: MonitoringStrings.thirtyDays,
                    value: TimespansMs.thirtyDays,
                },
            ],
            value: TimespansMs.oneHour, // default timespan
        });

        // MonitorChartV2
        const chartConfig = options.chartConfig;
        this._monitorChart = MonitorChartV2.create(container, {
            title: chartConfig.title,
            metrics: chartConfig.metrics,
            filterCollection: chartConfig.filterCollection,
            grouping: chartConfig.grouping,
        });
        ko.pureComputed(() => {
            const timespanMs = timeSelector.value();
            return {
                relative: {
                    duration: timespanMs,
                },
            };
        }).subscribeAndRun(container, (newTimespan) => {
            this._monitorChart.timespan(newTimespan);
        });

        this.control = CustomHtml.create(container, {
            htmlTemplate: HtmlTemplate,
            innerViewModel: {
                timeSelector: Section.create(container, {
                    children: [
                        timeSelector,
                    ],
                    leftLabelPosition: true,
                }),
                monitorChart: this._monitorChart,
            },
        });
    }

    public refresh(): void {
        // Work around to force refresh on charts
        const currentTimespan = MsPortalFx.clone(this._monitorChart.timespan());
        this._monitorChart.timespan(currentTimespan);
    }
}