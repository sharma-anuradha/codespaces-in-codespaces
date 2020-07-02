import * as ClientResources from 'ClientResources';
import { batch } from 'Fx/Ajax';
import { BladeReferences, PartReferences } from 'Fx/Composition';
import * as TemplateBlade from 'Fx/Composition/TemplateBlade';
import * as Dialog from 'Fx/Composition/Dialog';
import {
    createDefaultResourceLayout,
    ResourceLayoutContract,
    Item,
    MultiLineItem,
} from 'Fx/Controls/Essentials';
import { Links, ResourceMenuBladeIds } from '../../../Shared/Constants';
import { trace } from '../../../Shared/Logger';
import * as InfoTilesComponent from './InfoTilesComponent/InfoTilesComponent';
import * as Toolbar from 'Fx/Controls/Toolbar';

import Images = MsPortalFx.Base.Images;
import { HttpPlansManager } from 'Resource/HttpPlansManager';
import { HttpCodespacesManager } from 'Resource/Blades/Codespaces/HttpCodespacesManager';
import { Codespace } from 'Resource/Blades/Codespaces/CodespaceModels';

/**
 * Contract for parameters that will be passed to overview blade.
 */
export interface Parameters {
    //subscription: FxSubscriptionDropDown.Subscription;
    id: string;
}

/**
 * Overview blade provides the overview of resource on resource menu.
 * Learn more about decorator based blades at: https://aka.ms/portalfx/nopdl
 */
/* inline HTML */
@TemplateBlade.Decorator({
    htmlTemplate:
        "<div data-bind='pcControl: essentialsViewModel'></div>" +
        "<h3 class='ext-overview-title' data-bind='text: description'></h3>" +
        "<div class='msportalfx-padding'>" +
        "<div data-bind='pcControl: infoTilesComponent.control'></div>" +
        '</div>',
    styleSheets: ['./ResourceOverviewBlade.css', './InfoTilesComponent/InfoTilesComponent.css'],
})
@TemplateBlade.Pinnable.Decorator()
export class ResourceOverviewBlade {
    private _codespacesManager: HttpCodespacesManager;
    private _codespacesCount = ko.observable<string>('0');

    public infoTilesComponent: InfoTilesComponent.Contract;

    /**
     * The title of resource overview blade
     */
    public readonly title = ko.observable<string>(ClientResources.resourceOverviewBladeTitle);

    /**
     * The title of resource overview blade
     */
    public readonly subtitle = ClientResources.resourceOverviewBladeSubtitle;

    /**
     * The description of resource overview blade
     */
    public readonly description = ClientResources.Overview.description;

    /**
     * The context property contains APIs you can call to interact with the shell.
     * It will be populated for you by the framework before your onInitialize() function is called.
     *   https://aka.ms/portalfx/nopdl/context
     */
    public readonly context: TemplateBlade.Context<Parameters>;

    /**
     * The essentials control. Learn more at https://aka.ms/portalfx/controls/essentials
     */
    public readonly essentialsViewModel = ko.observable<ResourceLayoutContract>();

    /**
     * Initializes everything you need to load the blade here.
     */
    public onInitialize() {
        trace('ResourceOverviewBlade', 'Initialize blade');

        const id = this.context.parameters.id;
        this._codespacesManager = new HttpCodespacesManager(id);

        this.essentialsViewModel(
            createEssentials(this.context.container, id, this._additionalEssentialData())
        );
        this.context.container.commandBar = createToolbar(this.context.container, id, this);

        // Fill out the body of the page - should format be "createBody" function??
        // Set the blade contents.  This code uses InfoTiles and Monitoring components

        // InfoTilesComponent
        const OpenCodespaceTile: InfoTilesComponent.InfoTile = this._createOpenCodespaceTile();
        const ConfigureVirtualNetworksTile: InfoTilesComponent.InfoTile = this._createConfigureVirtualNetworksTile();
        const ChangeCodespaceConfigurationTile: InfoTilesComponent.InfoTile = this._createChangeCodespaceConfigurationTile();

        // InfoTilesComponent
        this.infoTilesComponent = InfoTilesComponent.create(this.context.container, {
            infoTiles: [
                OpenCodespaceTile,
                ConfigureVirtualNetworksTile,
                ChangeCodespaceConfigurationTile,
            ],
        });

        this.context.container.revealContent();

        this._codespacesManager
            .fetchCodespaces()
            .then((codespaces: Codespace[]) => this._codespacesCount(codespaces.length.toString()));

        return new HttpPlansManager().fetchPlan(this.context.parameters.id).then((result) => {
            this.title(result.name);
        });
    }

    public onPin() {
        return PartReferences.forPart('ResourcePart').createReference({
            parameters: {
                id: this.context.parameters.id,
            },
        });
    }

    // Internal Functionality

    /*
     * Additional essential data
     */
    private _additionalEssentialData(): (Item | MultiLineItem)[] {
        return [
            {
                label: ClientResources.Overview.codespacePlanNameLabel,
                value: this.title,
            },
            {
                label: ClientResources.Overview.codespacesLabel,
                value: this._codespacesCount,
                onClick: () => {
                    this.context.menu.switchItem(ResourceMenuBladeIds.codespacesItem);
                },
            },
            {
                label: ClientResources.Overview.virtualNetworkLabel,
                value: ClientResources.Overview.virtualNetworkLabel,
                onClick: () => {},
            },
        ];
    }

    /*
     * Tile that switches to Codespaces Blade.
     */
    private _createOpenCodespaceTile(): InfoTilesComponent.InfoTile {
        const switchToCodespacesBlade = () => {
            this.context.menu.switchItem(ResourceMenuBladeIds.codespacesItem);
        };

        return {
            title: {
                text: ClientResources.Tile.addCodespaceTitle,
                onClick: () => {
                    switchToCodespacesBlade();
                },
            },
            description: ClientResources.Tile.addCodespaceDescription,
            icon: Images.Polychromatic.JourneyHub(),
            links: [
                {
                    text: ClientResources.learnMore,
                    uri: Links.visualStudioCodespaces,
                },
                {
                    text: ClientResources.Tile.addCodespaceLinkTitle,
                    onClick: () => {
                        switchToCodespacesBlade();
                    },
                },
            ],
        };
    }

    /*
     * Tile that opens Virtual Network blade in a context pane
     */
    private _createConfigureVirtualNetworksTile(): InfoTilesComponent.InfoTile {
        const openVirtualNetworkBlade = () => {
            this.context.container.openContextPane(
                BladeReferences.forBlade('ResourceKeysBlade').createReference({
                    parameters: {
                        id: this.context.parameters.id,
                    },
                })
            );
        };

        return {
            title: {
                text: ClientResources.Tile.configureVirtualNetworksTitle,
                onClick: () => {
                    openVirtualNetworkBlade();
                },
            },
            description: ClientResources.Tile.configureVirtualNetworksDescription,
            icon: Images.Polychromatic.JourneyHub(),
            links: [
                {
                    text: ClientResources.Tile.configureVirtualNetworksLinkTitle,
                    onClick: () => {
                        openVirtualNetworkBlade();
                    },
                },
            ],
        };
    }

    /*
     * Tile that opens Visual Studio Codespaces Enviroments external link.
     */
    private _createChangeCodespaceConfigurationTile(): InfoTilesComponent.InfoTile {
        return {
            title: {
                text: ClientResources.Tile.changeCodespaceConfigurationTitle,
                uri: Links.visualStudioCodespacesEnvironments,
            },
            description: ClientResources.Tile.changeCodespaceConfigurationDescription,
            icon: Images.Polychromatic.JourneyHub(),
            links: [
                {
                    text: ClientResources.Tile.changeCodespaceConfigurationLinkTitle,
                    uri: Links.visualStudioCodespacesEnvironments,
                    useButtonDesign: true,
                },
            ],
        };
    }
}

/**
 * Creates an essentials view model.
 */
function createEssentials(
    container: TemplateBlade.Container,
    resourceId: string,
    additionaldata?: (Item | MultiLineItem)[]
): ResourceLayoutContract {
    const essentials = createDefaultResourceLayout(container, {
        resourceId: resourceId,
        includeTags: true,
        expanded: ko.observable(true),
        additionalRight: additionaldata,
    });
    return essentials;
}

/**
 * Creates a toolbar/commandbar.
 */
function createToolbar(
    container: TemplateBlade.Container,
    resourceId: string,
    ROBlade: ResourceOverviewBlade
): Toolbar.Contract {
    /* Use the built-in move button */
    const commandMoveButton = Toolbar.ToolbarItems.createMoveResourceButton(container, {
        resourceId: resourceId,
    });

    /* Delete button should open a Yes/No dialg */
    const commandDeleteButton = Toolbar.ToolbarItems.createBasicButton(container, {
        label: ClientResources.delete,
        /* Use built-in portal images for common commands */
        icon: Images.Delete(),
        onClick: () => {
            container.openDialog({
                title: ClientResources.delete,
                content: ClientResources.deleteConfirmation,
                buttons: Dialog.DialogButtons.YesNo,
                telemetryName: 'DeleteMyResource',
                onClosed: (result) => {
                    if (result.button === Dialog.DialogButton.Yes) {
                        /* call your delete function here */
                    }
                },
            });
        },
    });

    const commandRefreshButton = Toolbar.ToolbarItems.createBasicButton(container, {
        label: ClientResources.refresh,
        /* Images, use Flat Icons in toolbars */
        icon: Images.Refresh(),
        onClick: () => {},
    });

    const commandBar = Toolbar.create(container, {
        items: [commandMoveButton, commandDeleteButton, commandRefreshButton],
    });
    return commandBar;
}
