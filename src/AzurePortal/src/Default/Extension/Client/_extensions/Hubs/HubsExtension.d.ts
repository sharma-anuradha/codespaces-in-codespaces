// import { PdlBladeReference } from "Fx/Composition/Selectable";

/**
 * Hubs extension export declarations for blade and part parameters, outputs and settings.
 */
declare namespace HubsExtension {
    /**
     * Declarations for the ResourcePropertiesBlade.
     */
    namespace ResourcePropertiesBlade {
        /**
         * Input parameters for the resource properties blade.
         */
        interface Parameters {
            /**
             * Resource ID.
             */
            readonly id: string;

            /**
             * Optional to to override the default resource icon.
             */
            readonly overrideIcon?: boolean;
        }
    }

    /**
     * Declarations for the ResourceGroupMapBlade.
     */
    namespace ResourceGroupMapBlade {
        /**
         * Input parameters for the resource group map blade.
         */
        interface Parameters {
            /**
             * Resource group ID.
             */
            readonly id: string;
        }
    }

    /**
     * Declarations for the SharedQueryPropertiesBlade.
     */
    namespace SharedQueryPropertiesBlade {
        /**
         * Input parameters for the shared query overview blade.
         */
        interface Parameters {
            /**
             * Shared query ID.
             */
            readonly id: string;
        }
    }

    /**
     * Declarations for the ResourceTagsBlade.
     */
    namespace ResourceTagsBlade {
        /**
         * Input parameters for the resource tags blade.
         */
        interface Parameters {
            /**
             * Resource ID for the resource to be edited.
             */
            readonly resourceId: string;
        }
    }

    /**
     * Declarations for the EditTagsBlade.
     */
    namespace EditTagsBlade {
        /**
         * Input parameters for the edit tags blade.
         */
        interface Parameters {
            /**
             * Resource ID for the resource to be edited.
             */
            readonly resource: string;

            /**
             * Optional tag name for the tag to edit first.
             */
            readonly tagName?: string;
        }

        /**
         * Output results for the edit tags blade.
         */
        interface Results {
            /**
             * Value which indicates that the resource should be updated.
             */
            readonly update: boolean;

            /**
             * If the update flag is set, this will be the new tags returned by ARM.
             */
            readonly newTags?: StringMap<string>;

            /**
             * This is a serialized version of the newTags property, which is safe to send across the PO layer.
             */
            readonly newTagsSerialized?: string;
        }
    }

    /**
     * Declarations for the AssignTagsBlade.
     */
    namespace AssignTagsBlade {
        /**
         * Input parameters for the assign tags blade.
         */
        interface Parameters {
            /**
             * Array of resource IDs for the assign.
             */
            readonly resources: string[];

            /**
             * Optional array of pre-populated tags for the assign.
             */
            readonly tags?: { name: string, value: string }[];
        }

        /**
         * Output results for the assign tags blade.
         */
        interface Results {
            /**
             * Array of resource IDs that need to be updated.
             */
            readonly update: StringMap<boolean>;

            /**
             * If the update map contains a resource ID, this map will point the resourceId to the new tags returned by ARM.
             */
            readonly newTags?: StringMap<StringMap<string>>;
        }
    }

    /**
     * Declarations for the TagsBlade
     */
    namespace TagsBlade {
        /**
         * Input parameters for the tags blade.
         */
        interface Parameters {
            /**
             * Optional subscription ID for the tags blade.
             */
            readonly subscriptionId?: string;
        }
    }

    namespace MonitorChartPart {
        /**
         * Defines how data points are aggregated for a metric.
         * @deprecated MonitorChartPart on longer supports this AggregationType format. Use the chart parameter instead which uses the ChartOptions type
         */
        const enum AggregationType {
            /**
             * No aggregation is done.
             */
            None,
            /**
             * Data points are aggregated by taking the average of their values.
             */
            Average,
            /**
             * Data points are aggregated by taking the min of their values.
             */
            Minimum,
            /**
             * Data points are aggregated by taking the max of their values.
             */
            Maximum,
            /**
             * Data points are aggregated by taking the total of their values.
             */
            Total
        }

        /**
         * Defines what visualization to use when rendering the chart.
         * @deprecated MonitorChartPart on longer supports this ChartType format. Use the chart parameter instead which uses the ChartOptions type
         */
        const enum ChartType {
            /**
             * Line chart.
             */
            Line,
            /**
             * Bar chart.
             */
            Bar
        }

        /**
         * Defines the unit of a metric.
         * @deprecated MonitorChartPart on longer supports this Unit format. Use the chart parameter instead which uses the ChartOptions type
         */
        const enum Unit {
            /**
             * Count unit.
             */
            Count,
            /**
             * Bytes unit.
             */
            Bytes,
            /**
             * Seconds unit.
             */
            Seconds,
            /**
             * CountPerSecond unit.
             */
            CountPerSecond,
            /**
             * BytesPerSecond unit.
             */
            BytesPerSecond,
            /**
             * Percent unit.
             */
            Percent,
            /**
             * MilliSeconds unit.
             */
            MilliSeconds,
            /**
             * ByteSeconds unit.
             */
            ByteSeconds
        }

        /**
         * Defines the timespan over which data points are fetched and plotted.
         * @deprecated MonitorChartPart on longer supports this Timespan format. Use the chart parameter instead which uses the ChartOptions type
         */
        interface Timespan {
            /**
             * A relative timespan indicates that data points are plotted for a moving timespan, whose
             * end time is always now().
             */
            relative?: {
                /**
                 * Length of time over which metrics are plotted.
                 */
                durationMs: number;
            };

            /**
             * An absolute timespan indicates that data points are plotted for a fixed timespan.
             */
            absolute?: {
                /**
                 * The start time of the timespan.
                 */
                start: Date;

                /**
                 * The end time of the timespan.
                 */
                end: Date;
            };

            /**
             * How frequently the data points shown on the chart should be updated.
             */
            refreshIntervalMs?: number;
        }

        /**
         * @deprecated MonitorChartPart on longer supports this Resource format. Use the chart parameter instead which uses the ChartOptions type
         */
        interface ResourceMetadata {
            /**
             * The resource id of the resource.
             */
            resourceId: string;

            /**
             * The kind of the resource.
             */
            kind?: string;

            /**
             * The sku of the resource.
             */
            sku?: {
                /**
                 * The sku name.
                 */
                name?: string;

                /**
                 * The sku tier.
                 */
                tier?: string;

                /**
                 * The sku size.
                 */
                size?: string;

                /**
                 * The sku family.
                 */
                family?: string;

                /**
                 * The sku model.
                 */
                model?: string;

                /**
                 * The sku capacity.
                 */
                capacity?: string;
            };

            /**
             * Additional properties for the resource.
             */
            properties?: any;
        }

        /**
         * @deprecated MonitorChartPart on longer supports this Dimension format. Use the chart parameter instead which uses the ChartOptions type
         */
        interface Dimension {
            /**
             * The dimension name.
             */
            name: string;

            /**
             * The dimension value.
             */
            value: string;
        }

        /**
         * @deprecated MonitorChartPart on longer supports this Metric format. Use the chart parameter instead which uses the ChartOptions type
         */
        interface Metric {
            /**
             * Resource information about the resource to which this metric belongs.
             */
            resourceMetadata: ResourceMetadata;

            /**
             * The non-localized metric name.
             */
            name: string;

            /**
             * The dimensions for this metric.
             *
             * Note: Separate time series are plotted for each dimension.
             */
            dimensions?: Dimension[];

            /**
             * The aggregation type to use for this metric.
             */
            aggregationType?: AggregationType;

            /**
             * The time grain to use for this metric.
             * @deprecated This property is ignored in favor of the grain specified in the chart timespan
             */
            timeGrainMs?: number;

            /**
             * The unit of this metric.
             * @deprecated This property is ignored in favor of respecting the unit reported by the backend metadata.
             */
            unit?: Unit;

            /**
             * Helps in locating correct MetricsProvider.
             * TODO: add external documentation explaining when to use this type property
             */
            type?: string;
        }

        /**
         * @deprecated MonitorChartPart on longer supports this Chart format. Use the chart parameter instead which uses the ChartOptions type
         */
        interface ChartDefinition {
            /**
             * The visualization to use for this chart.
             */
            chartType?: ChartType;

            /**
             * The timespan to use for this chart.
             *
             * Note: This overrides the top-level timespan provided in the Options object.
             */
            timespan?: Timespan;

            /**
             * The metrics to plot on this chart.
             */
            metrics: Metric[];

            /**
             * The title of this chart.
             */
            title?: string;

            /**
             * The subtitle of this chart.
             */
            subtitle?: string;

            /**
             * Message to display if when no metrics are available.
             */
            noMetricsMessage?: string;

            /**
             * Disables pinning for this chart.
             */
            disablePinning?: boolean;

            /**
             * Css class to apply to this chart.
             *
             * Multiple css classes can be added by separating them with a space.
             */
            cssClass?: string;
        }

        /**
         * This namespace contains types related to a metric being plotted on the chart
         */
        namespace Metrics {
            /**
             *  Describes the ARM resource whose metric is being plotted on the chart.
             */
            interface ResourceMetadata {
                /**
                 * The ARM resource id of the resource.
                 */
                id: string;
                /**
                 * The resource kind of the resource.
                 * This is optional and makes fetching the resource faster.
                 * When this is not specified it is fetched from ARM and populated.
                 */
                kind?: string;
                /**
                 * The sku of the resource.
                 * These optional features makes fetching the resource faster.
                 * When this is not specified it is fetched from ARM and populated.
                 */
                sku?: {
                    /**
                     * The sku name.
                     */
                    name?: string;
                    /**
                     * The sku tier.
                     */
                    tier?: string;
                    /**
                     * The sku size.
                     */
                    size?: string;
                    /**
                     * The sku family.
                     */
                    family?: string;
                    /**
                     * The sku model.
                     */
                    model?: string;
                    /**
                     * The sku capacity.
                     */
                    capacity?: string;
                };
            }

            /**
             *  Describes the scope of the metric is being plotted on the chart.
             *  Use Scope when creating a chart with cross-resource queries.
             */
            export interface CrossResourceMetadata {
                /**
                 * The subscription to query metrics for.
                 */
                subscription: Pick<MsPortalFx.Azure.Subscription, "subscriptionId" | "uniqueDisplayName">;
                /**
                 * The resource type to query metric for.
                 * For example, "microsoft.compute/virtualmachines" or "microsoft.keyvault/vault"
                 */
                resourceType: string;
                /**
                 * The region in which to query the resource type.
                 * For example, "westus2"
                 */
                region: string;
            }

            /**
             * Aggregation to use for the metric values
             */
            const enum AggregationType {
                /**
                 * No aggregation. Invalid value.
                 */
                None = 0,
                /**
                 * Sum of metric values per bucket.
                 */
                Sum = 1,
                /**
                 * Minimum of metric values per bucket.
                 */
                Min = 2,
                /**
                 * Maximum of metric values per bucket.
                 */
                Max = 3,
                /**
                 * Average of metric values per bucket.
                 */
                Avg = 4,
                /**
                 * Unique count of metric values per bucket.
                 */
                Unique = 5,
                /**
                 * 90th Percentile of metric values per bucket.
                 */
                Percentile = 6,
                /**
                 * Count of metric values per bucket.
                 */
                Count = 7,
                /**
                 * For classic storage metric which only supports 'Last' aggregation
                 */
                Last = 8,
            }

            /**
             * Visualization options related to a metric on the chart
             */
            interface Visualization {
                /**
                 * The localized metric display name to be displayed on the chart.
                 * When not specified the metric id is used to display on the chart.
                 */
                displayName?: string;
                /**
                 * The localized resource display name to be displayed on the chart
                 * When not specified the ARM resource name is displayed on the chart.
                 */
                resourceDisplayName?: string;
                /**
                 * Color of the metric when plotted on the chart, in hexadecimal format (e.g. #c7f1c7).
                 * NOTE: Make sure the colors work in dark and light themes.
                 * When unspecified, chart chooses a random color.
                 */
                color?: string;
            }

            /**
             * Describes the properties of a threshold line on the metric plotted on chart.
             */
            interface ThresholdOptions {
                /**
                 * The identifier for the threshold.
                 * Each threshold on a metric has a unique identifier.
                 * The id can also be used to identify and access a threshold from `Metric.Threshold` array.
                 * If not provided a default one is created.
                 */
                id: string;

                /**
                 * The upper bound of the threshold line. Defaults to 0.
                 */
                upperThreshold?: number;

                /**
                 * The lower bound of the threshold. Defaults to 0.
                 */
                lowerThreshold?: number;
            }

            /**
             * Options for creating a metric.
             */
            interface Options {
                /**
                 * Information that identifies the resource to which the metric belongs.
                 */
                resourceMetadata: ResourceMetadata | CrossResourceMetadata;
                /**
                 * The name of the metric.
                 */
                name: string;
                /**
                 * The aggregation type to use for this metric.
                 */
                aggregationType: AggregationType;
                /**
                 * Additional information to use when determining the correct metrics provider.
                 * For example, VM providers need to use this, because they can have multiple providers for
                 * one VM resource (i.e. Host and Guest metrics).
                 * This is only needed for certain providers and not all
                 * When not specified, chart uses a fixed default for each metric type.
                 */
                namespace?: string;
                /**
                 * Visualization options for the metric.
                 * Defaults to a fixed visualization options on all metrics.
                 */
                metricVisualization?: Visualization;
                /**
                 * Threshold options for the metric.
                 * This is used to place threshold lines on the chart for the metric.
                 * When not specified, there are no threshold lines placed on the chart.
                 */
                thresholds?: ThresholdOptions[];
            }
        }

        /**
         * A collection of all the filters on the chart.
         */
        interface FilterCollection {
            /**
             * Array containing all the Filters applied to the chart.
             */
            filters: Filter[];
        }

        /**
         * Operators for filter comparisons.
         */
        const enum FilterComparisonOperator {
            Equal = 0,
            NotEqual = 1,
            Contains = 2,
            StartsWith = 3,
        }

        /**
         * Describes an individual filter which can be added to the FilterCollection of the chart.
         */
        interface Filter {
            /**
             * The dimension key for the filter.
             */
            key: string;
            /**
             * Filter operator to be applied to the values (= or ≠)
             * Defaults to equal.
             */
            operator?: number;
            /**
             * The set of values for the key we want to filter against.
             */
            values: string[];
        }

        /**
         * Describes the grouping/segmentation of the metric on the chart.
         */
        interface Grouping {
            /**
             * The dimension to group against.
             */
            dimension: string;
        }

        /**
         * This namespace contains types related to a the display of the chart
         */
        namespace ChartVisualization {
            /**
             * List of supported chart types.
             */
            const enum ChartType {
                /**
                 * Bar Chart.
                 */
                Bar = 1,
                /**
                 * Line Chart.
                 */
                Line = 2,
                /**
                 * Area Chart.
                 */
                Area = 3,
                /**
                 * Scatter plot chart.
                 */
                Scatter = 4
            }
            /**
             * Represents how to display a chart's axis.
             */
            interface AxisVisualization {
                /**
                 * x-axis visualization options.
                 */
                x?: IndividualAxisVisualization;
                /**
                 * y-axis visualization options.
                 */
                y: IndividualAxisVisualization;
            }
            /**
             * Visualization options for a particular axis.
             */
            interface IndividualAxisVisualization {
                /**
                 * Determines if the axis is visible on the chart or hidden.
                 */
                isVisible: boolean;
                /**
                 * Defines the smallest value shown for the axis.
                 * Defaults to '0' for numeric axis and start timespan for date axis.
                 */
                min?: number;
                /**
                 * Defines the largest value shown for the axis.
                 * Defaults to maximum data value of the plotted data for numeric axis and end timespan for date axis.
                 */
                max?: number;
            }
            /**
             * List of relative positions to place legend.
             */
            const enum LegendPosition {
                /**
                 * Legends placed on bottom of chart.
                 */
                Bottom = 2,
                /**
                 * Legends placed on right of chart.
                 */
                Right = 4
            }
            /**
             * Represents how to style a chart's legend.
             */
            interface LegendVisualization {
                /**
                 * Controls if the legend is visible or hidden on the chart.
                 */
                isVisible: boolean;
                /**
                 * Controls the position of legend with respect to the chart.
                 * Default is to position on bottom of chart.
                 */
                position?: LegendPosition;
            }
            /**
             * Represents the visual elements of a chart.
             */
            interface Options {
                /**
                 * The type of chart to be used. Defaults to a line chart.
                 */
                chartType?: ChartType;

                /**
                 * Options related to how the legend should be displayed.
                 * Defaults to a fixed legend visualization with legends on bottom.
                 */
                legendVisualization?: LegendVisualization;

                /**
                 * Options related to how the axis should be displayed.
                 * Defaults to a fixed axis visualization.
                 */
                axisVisualization?: AxisVisualization;

                /**
                 * Indicates whether users can pin this chart to their dashboard.
                 * Defaults to false and pin button is always shown.
                 */
                disablePinning?: boolean;
            }
        }

        /**
         * This namespace contains related to the time range and time grain of the chart
         */
        namespace Time {
            /**
             * Time range over which data points are fetched and plotted.
             */
            interface TimeRange {
                /**
                 * The start of the time range.
                 */
                startTime: Date;
                /**
                 * The end of the time range.
                 */
                endTime: Date;
            }
            /**
             * Supported grains that can be applied on a timespan for the chart.
             */
            const enum Grain {
                /**
                 * Chart chooses an automatic grain to display.
                 */
                Automatic = 1,
                /**
                 * Grain to set to per minute over the given time range.
                 */
                Minutely = 2,
                /**
                 * Grain to set to per hour over the given time range.
                 */
                Hourly = 3,
                /**
                 * Grain to set to per day over the given time range.
                 */
                Daily = 4,
                /**
                 * Grain to set to per week over the given time range.
                 */
                Weekly = 5,
                /**
                 * Grain to set to per month over the given time range.
                 */
                Monthly = 6,
                /**
                 * Grain to set to every 5 minutes over the given time range.
                 */
                Every_5_Minutes = 7,
                /**
                 * Grain to set to every 15 minutes over the given time range.
                 */
                Every_15_Minutes = 8,
                /**
                 * Grain to set to every 30 minutes over the given time range.
                 */
                Every_30_Minutes = 9,
                /**
                 * Grain to set to every 6 hours over the given time range.
                 */
                Every_6_Hours = 10,
                /**
                 * Grain to set to every 12 hours over the given time range.
                 */
                Every_12_Hours = 11
            }
            /**
             * Timespan signifies the absolute or relative time range for which the chart displays the data.
             */
            interface Timespan {
                /**
                 * The absolute time range of the chart's timespan.
                 * If this is not present relative duration is used.
                 * If neither of absolute or relative is specified, chart defaults to past 24 hours time range.
                 */
                absolute?: TimeRange;
                /**
                 * The related duration for the chart's timespan.
                 * If this is not present absolute duration is used.
                 * If neither of absolute or relative is specified, chart defaults to past 24 hours time range.
                 */
                relative?: {
                    duration: number;
                };
                /**
                 * The grain of the time context.
                 * Defaults to a fixed grain with respect to the timespan.
                 */
                grain?: Grain;
                /**
                 * Set to true to display time as UTC on the chart
                 * Defaults to using local time.
                 */
                showUTCTime?: boolean;
            }
        }

        /**
         * MonitorChartV2 title kind.
         */
        const enum TitleKind {
            /** no title */
            None = 0,
            /** title is automatically generated */
            Auto = 1,
            /** title is customized */
            Custom = 2,
        }

        /**
         * Matches the options for a MonitorChartV2 control which is what MonitorChart is using today.
         */
        interface ChartOptions {
            /**
             * The list of metrics to plot on this chart.
             */
            metrics: Metrics.Options[];

            /**
             * The filters to apply on the chart for all metrics.
             * If not specified, data isn't filtered.
             */
            filterCollection?: FilterCollection;

            /**
             * Specify the grouping to segment the chart against.
             * If not specified chart is not segmented.
             */
            grouping?: Grouping;

            /**
             * Controls the display properties of chart.
             */
            visualization?: ChartVisualization.Options;

            /**
             * Title for the chart. Defaults to no title.
             */
            title?: string;

            /**
             * Specifies the kind of title to be displayed.
             * Defaults to 'Custom' if 'title' is supplied. Otherwise, defaults to 'None'.
             */
            titleKind?: TitleKind;

            /**
             * The timespan used for the MonitorChartV2
             * Defaults dashboard time or past 24 hours.
             */
            timespan?: Time.Timespan;

            /**
             * Aria label for the MonitorChartV2. Defaults to "MonitorChartV2"
             */
            ariaLabel?: string;
        }

        /**
         * MonitorChart options.
         */
        interface Options {
            /**
             * Chart to render.
             */
            chart?: ChartOptions;

            /**
             * The charts to render.
             * NOTE: MonitorChartPart on longer supports multiple charts. Only the 1st chart in the array is respected.
             * @deprecated MonitorChartPart on longer supports multiple charts. Only the 1st chart in the array is respected. Callers should use the chart property instead.
             */
            charts?: ChartDefinition[];

            /**
             * The timespan used for all charts, unless overridden in an individual chart.
             *
             * Defaults to relative duration of 24 hours if not specified.
             *
             * @deprecated Callers should the timespan property on ChartDefinition instead.
             */
            timespan?: Timespan;
        }

        /**
         * Inputs to the part
         */
        interface Parameters {
            /**
             * The inputs to the chart control
             */
            options?: Options;

            /**
             * This input is deprecated and ignored. Callers should the timespan property on ChartDefinition instead.
             * @deprecated Callers should the timespan property on ChartDefinition instead.
             */
            sharedTimeRange?: MsPortalFx.Composition.Configuration.TimeRange;
        }
    }

    /**
     * Declarations for the DeploymentDetailsBlade.
     */
    namespace DeploymentDetailsMenuBlade {
        /**
         * The inputs to the blade.
         */
        interface Parameters {
            /**
             * The deployment id.
             */
            readonly id: string;

            /**
             * The gallery package Id. Optional to maintain back-compat.
             */
            readonly packageId?: string;

            /**
             * The absolute URI of the medium icon image for the gallery item.
             */
            readonly packageIconUri?: string;

            /**
             * The ID of the primary resource. Optional to maintain back-compat.
             */
            readonly primaryResourceId?: string;

            /**
             * A key or hash that encodes or corresponds to information about a provisioning request.
             * If this is provided, the packageId parameter must also be provided.
             */
            readonly provisioningHash?: string;

            /**
             * Metadata about the blade that initiated the deployment.
             */
            readonly createBlade?: {
                /**
                 * The name of the create blade that initiated the deployment.
                 */
                readonly bladeName: string;
                /**
                 * The name of the extension containing the create blade that initiated the deployment.
                 */
                readonly extension: string;
            }
        }
    }

    /**
     * Declarations for the TemplateEditorBladeV2.
     */
    namespace TemplateEditorBladeV2 {
        /**
         * The inputs to the blade.
         */
        interface Parameters {
            /**
             * Read only flag.
             */
            readonly readOnlyTemplate?: boolean;

            /**
             * The input template.
             */
            readonly template?: string;
        }

        /**
         * The blade results.
         */
        interface Results {
            /**
             * The output template.
             */
            readonly template?: string;
        }
    }

    /**
     * Declarations for the BrowseAll blade.
     */
    namespace BrowseAll {
        /**
         * The blade parameters.
         */
        export interface Parameters {
            /**
             * Optional filter (search) input that is applied on blade load.
             */
            readonly filter?: string;

            /**
             * Optional tag name input to pre-populate the tag filter.
             */
            readonly tagName?: string;

            /**
             * Optional tag value input to pre-populate the tag filter.
             */
            readonly tagValue?: string;
        }
    }

    /**
     * Declarations for the BrowseResource blade.
     */
    namespace BrowseResource {
        /**
         * The blade parameters.
         */
        export interface Parameters {
            /**
             * The ARM resource type, for example Microsoft.Compute/virtualMachines.
             */
            readonly resourceType: string;

            /**
             * Optional filter (search) input that is applied on blade load.
             */
            readonly filter?: string;

            /**
             * Optional kind (filter-to-kind) input that is applied on blade load.
             */
            readonly kind?: string;
        }
    }

    /**
     * Declarations for the BrowseResourcesWithTag blade.
     */
    namespace BrowseResourcesWithTag {
        /**
         * The blade parameters.
         */
        export interface Parameters {
            /**
             * Tag name input to pre-populate the tag filter.
             */
            readonly tagName: string;

            /**
             * Tag value input to pre-populate the tag filter.
             */
            readonly tagValue: string;

            /**
             * Optional filter (search) input that is applied on blade load.
             */
            readonly filter?: string;
        }
    }

    /**
     * Declarations for the BrowseQuery blade.
     */
    namespace BrowseQuery {
        /**
         * The blade parameters.
         */
        export interface Parameters {
            /**
             * The query for the browse.
             */
            readonly query: string;

            /**
             * The title for the browse.
             */
            readonly title: string;
        }
    }

    /**
     * Declarations for the ARG Query blade.
     */
    namespace ArgQueryBlade {
        /**
         * The supported chart types.
        */
        export const enum ChartType {
            None = 0,
            BarChart = 1,
            DonutChart = BarChart << 1,
            Map = DonutChart << 1,
            Grid = Map << 1,
        }

        /**
         * The parameters for the blade.
         */
        export interface Parameters {
            /**
             * The name of the query.
             */
            readonly name?: string;

            /**
             * The input query.
             */
            readonly query?: string;

            /**
             * The input query id.
             */
            readonly queryId?: string;

            /**
             * The input query description.
             */
            readonly description?: string;

            /**
             * The shared property.
             */
            readonly isShared?: boolean;

            /**
             * Flag indicating whether the results should be formatted by default.
             */
            readonly formatResults?: boolean;

            /**
             * Inputs if this blade is opened from a pinned part.
             */
            readonly fromPinnedPart?: {
                /**
                 * The chart type that opened the blade.
                 */
                readonly chartType?: HubsExtension.ArgQueryBlade.ChartType;

                /**
                 * The name of the query.
                 */
                readonly name?: string;

                /**
                 * The query from the part.
                 */
                readonly query?: string;

                /**
                 * The queryId from the part.
                 */
                readonly queryId?: string;

                /**
                 * The filter predicate based on the dashboard filters.
                 */
                readonly dashboardFilterQuery?: string;

                /**
                 * The list of selected subs from the dashboard filters.
                 */
                readonly selectedSubs?: ReadonlyArray<string>;

                /**
                 * The shared property
                 */
                readonly isShared?: boolean;
            };
        }

        export interface QueryBladeResult {
            /**
             * The chart type.
             */
            readonly chartType?: ChartType;

            /**
             * The name of the query.
             */
            readonly partTitle?: string;

            /**
             * If this parameter is set, then the part should open the full view.
             */
            readonly reopenInFullView?: {
                /**
                 * The name of the query.
                 */
                readonly name?: string;

                /**
                 * The query.
                 */
                readonly query?: string;

                /**
                 * The input query id
                 */
                readonly queryId?: string;

                /**
                 * The shared property
                 */
                readonly isShared?: boolean;
            };

            /**
             * The query content.
             */
            readonly query?: string;

            /**
             * Flag indicating whether or not the results of the query should be formatted.
             */
            readonly formatResults?: boolean;
        }

    }

    /**
     * Declarations for the In Product Feedback blade.
     */
    namespace InProductFeedbackBlade {
        /**
         * The parameters for the blade.
         */
        export interface Parameters {
            /**
             * The extension instantiating this blade.
             */
            readonly bladeName: string;

            /**
             * The question string to be displayed for the customer effort score section.
             */
            readonly cesQuestion: string;

            /**
             * The question string to be displayed for the customer value add section.
             */
            readonly cvaQuestion: string;

            /**
             * The extension instantiating this blade.
             */
            readonly extensionName: string;

            /**
             * The name of the feature the extension is surveying for.
             */
            readonly featureName: string;

            /**
             * The id for the specific survey.
             */
            readonly surveyId: string;
        }
    }
}
