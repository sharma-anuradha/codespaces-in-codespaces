import * as ClientResources from 'ClientResources';
import { BladeReferences } from 'Fx/Composition';
import Images = MsPortalFx.Base.Images;
import FxAssets = MsPortalFx.Assets;
import FxBase = MsPortalFx.Base;
import ResourceColumns = MsPortalFx.Assets.ResourceColumnIds;
import { ResourceMenuBladeIds } from '../Shared/Constants';
import { Icons } from '../Shared/Icons';
import * as Di from 'Fx/DependencyInjection';
import { initializeLogger } from '../Shared/Logger';

/**
 * ResourceAssetType that implement Resource Menu and Browse Contract
 * Learn more about Browse: https://aka.ms/portalfx/browse
 * Learn more about Resource Menu: https://aka.ms/portalfx/resourcemenu
 */

@Di.Class('viewModel')
export class PlansViewModel
    implements
        FxAssets.ResourceMenuWithCallerSuppliedResourceContract,
        FxAssets.BrowseConfigContract {
    /**
     * The container contains APIs you can call to interact with the shell.
     */
    private _container: MsPortalFx.ViewModels.ContainerContract;

    /**
     * The context property contains APIs you can call to interact with the shell.
     * Learn more about: https://aka.ms/portalfx/datacontext
     */

    /**
     * Constructor for Asset Type
     * @param container
     * @param initialState
     * @param dataContext
     */
    constructor(container: MsPortalFx.ViewModels.ContainerContract) {
        initializeLogger();

        this._container = container;
    }

    /**
     * Specifies the Browse config
     */
    public getBrowseConfig(): FxBase.PromiseV<FxAssets.BrowseConfig> {
        return Q({
            columns: [],
            defaultColumns: [
                ResourceColumns.ResourceType,
                ResourceColumns.Location,
                ResourceColumns.Subscription,
            ],
        });
    }

    /**
     * The menu config for the Resource menu blade
     * @param resourceInfo The resource information
     */
    public getMenuConfig(
        resourceInfo: FxAssets.ResourceInformation
    ): FxBase.PromiseV<FxAssets.ResourceMenuConfig> {
        const overviewItem: FxAssets.MenuItem = {
            id: ResourceMenuBladeIds.overview, // menu item IDs must be unique, must not be localized, should not contain spaces and should be lowercase
            displayText: ClientResources.overview,
            enabled: ko.observable(true),
            keywords: ClientResources.overviewKeywords,
            icon: Icons.cloudService,
            supplyBladeReference: () => {
                return BladeReferences.forBlade('ResourceOverviewBlade').createReference({
                    parameters: { id: resourceInfo.resourceId },
                });
            },
        };

        // Build the resource menu config.
        const menuConfig: FxAssets.ResourceMenuConfig = {
            //Overview  - designated by setting defaultid
            overview: overviewItem,
            options: {
                //Access control - on by default for ARM resources
                //   enableRbac: true,
                //Activity Logs - on by default for ARM resources
                //    enableSupportEventLogs: true,
                //Tags - on by default for ARM resources
                enableTags: false,
                //Diagnose and solve problems - recommended to enable, must onboard, see https://aka.ms/portalfx/resourcemenu
                enableSupportTroubleshootV2: false,
                //
                //SETTINGS group
                //Properties - recommended for all resources
                enableProperties: true,
                //Locks - on by default for all ARM resources
                //    enableLocks: true,
                //Automation script - on by default for all ARM resources
                //    enableExportTemplate: true,
                //
                //MONITORING group - these items all require onboarding and are recommended for all Azure resources
                //Alerts
                enableAlerts: false,
                //Metrics
                enableMetrics: false,
                //Diagnostic settings
                enableDiagnostics: false,
                //Logs
                //enableLogs - requires onboarding - - https://aka.ms/portalfx/resourcemenu
                enableLogs: true,
                //Advisor recommendations - requires onboarding - https://aka.ms/portalfx/resourcemenu
                enableSupportResourceAdvisor: false,
                //SUPPORT + TROUBLESHOOTING group
                //Resource health
                enableSupportResourceHealth: false,
                //Create support request - on by default for all ARM resources
                //  enableSupportHelpRequest: true
            },
            groups: [],
            handledError: ko.observable(),
            fail: ko.observable(),
        };

        return Q(menuConfig);
    }
}
