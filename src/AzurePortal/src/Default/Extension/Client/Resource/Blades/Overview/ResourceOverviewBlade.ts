import * as ClientResources from 'ClientResources';
import { BladeReferences, PartReferences, ClickableLink } from 'Fx/Composition';
import * as DataGrid from 'Fx/Controls/DataGrid';
import * as TemplateBlade from 'Fx/Composition/TemplateBlade';
import * as Dialog from 'Fx/Composition/Dialog';
import * as Section from 'Fx/Controls/Section';
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
import { Codespace, provisioningLower, startingLower, shuttingDownLower } from 'Resource/Blades/Codespaces/CodespaceModels';

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
        "<div class='msportalfx-padding'>" +
        "<div data-bind='visible: hasNoCodespaces'>" +
            "<h3 class='ext-overview-title' data-bind='text: description'></h3>" +
            "<div data-bind='pcControl: infoTilesComponent.control'></div>" +
        "</div>" +
        "<div data-bind='visible: hasCodespaces'>" +  
            "<div class='msportalfx-form' data-bind='pcControl: codespaceGridSection'></div>" +
        "</div>" +
        '</div>',
    styleSheets: ['./ResourceOverviewBlade.css', './InfoTilesComponent/InfoTilesComponent.css'],
})
@TemplateBlade.Pinnable.Decorator()
export class ResourceOverviewBlade {
    private _codespacesManager: HttpCodespacesManager;
    private _codespacesCount = ko.observable<string>('0');
    public hasCodespaces = ko.observable(true);
    public hasNoCodespaces = ko.observable(false);

    public infoTilesComponent: InfoTilesComponent.Contract;

    //public codespacesGrid: DataGrid.Contract<Codespace, any>;

    public codespaceGridSection: Section.Contract;

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

    //noCodespaces = ko.observable(false);

    /**
     * Initializes everything you need to load the blade here.
     */
    public onInitialize() {
        trace('ResourceOverviewBlade', 'Initialize blade');

        const { parameters } = this.context;
        const id = parameters.id;
        this._codespacesManager = new HttpCodespacesManager(id);

        this.essentialsViewModel(
            createEssentials(this.context.container, id, this._additionalEssentialData())
        );

        this.codespaceGridSection = Section.create(this.context.container);
        
        const codespacesGrid = this._initializeCodespacesGrid();
        this.codespaceGridSection.children.push(codespacesGrid);

        this.context.container.commandBar = createToolbar(this.context.container, id, codespacesGrid, this._codespacesManager);

        // InfoTilesComponent  
        const OpenCodespaceTile: InfoTilesComponent.InfoTile = this._createOpenCodespaceTile(this.context.container, id, codespacesGrid,);
        this.infoTilesComponent = InfoTilesComponent.create(this.context.container, {
            infoTiles: [
                OpenCodespaceTile,
            ],
        });

        this.context.container.revealContent();

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
    private _createOpenCodespaceTile(
        container: TemplateBlade.Container,
        planId: string,
        codespacesGrid: DataGrid.Contract<Codespace>
    ): InfoTilesComponent.InfoTile {
        return {
            title: {
                text: ClientResources.Tile.addCodespaceTitle,
                onClick: () => {
                    openCreateBlade(container, planId, codespacesGrid);
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
                        openCreateBlade(container, planId, codespacesGrid);
                    },
                },
            ],
        };
    }

    private _initializeCodespacesGrid(): DataGrid.Contract<Codespace> {
        const dataSource = () =>
            this._codespacesManager.fetchCodespaces().then((codespaces: Codespace[]) => {
                codespaces
                    .filter((codespace) => codespace.state.toLowerCase() === provisioningLower)
                    .forEach(({ id }) =>
                        this._codespacesManager
                            .pollTransitioningCodespace(id)
                            .then(() => {
                                codespacesGrid.refresh()
                            })
                    );
                this._codespacesCount(codespaces.length.toString());
                this.hasNoCodespaces(codespaces.length === 0);
                this.hasCodespaces(codespaces.length > 0);
                return codespaces.map((codespace) => ({ id: codespace.id, item: codespace }));
            });

        const columns: DataGrid.ColumnDefinition<Codespace>[] = [
            {
                header: 'Name',
                type: 'BladeLink',
                cell: {
                    bladeLink: (c) => ({
                        text: c.friendlyName,
                        bladeReference: BladeReferences.forBlade('Connect.ReactView').createReference({
                            parameters: {
                                planId: this.context.parameters.id,
                                codespaceId: c.id,
                                codespacesEndpoint: MsPortalFx.getEnvironmentValue("codespacesEndpoint"),
                                armApiVersion: MsPortalFx.getEnvironmentValue("armApiVersion")
                            },
                            onClosed: () => {
                                codespacesGrid.refresh();
                            },
                        }),
                    }),
                },
            },
            {
                header: 'ID',
                type: 'Text',
                cell: {
                    text: (c) => c.id,
                },
            },
            {
                header: 'State',
                type: 'Text',
                cell: {
                    text: (c) => c.state,
                },
            },
        ];

        const codespacesGrid = DataGrid.create<Codespace>(this.context.container, {
            ariaLabel: 'codespaces-grid',
            columns,
            dataSource,
            noDataMessage: 'No Codespaces found for the plan',
            selection: {
                selectionMode: DataGrid.SelectionMode.Multiple,
                canSelectAllItems: () => true,
                canSelectItem: () => true,
                canUnselectAllItems: () => true,
                canUnselectItem: () => true,
            },
            contextMenu: {
                maxButtonCommands: 5,
                supplyMenuCommands: (lifetime, row) => {
                    const link = `ms-vsonline.vsonline/connect?environmentId=${encodeURIComponent(
                        row.id
                    )}`;
                    return [
                        Toolbar.ToolbarItems.createBasicButton(lifetime, {
                            label: 'Open in VS Code',
                            icon: Images.Hyperlink(),
                            onClick: new ClickableLink(`vscode://${link}`),
                        }),
                        Toolbar.ToolbarItems.createBasicButton(lifetime, {
                            label: 'Open in VS Code Insiders',
                            icon: Images.Hyperlink(),
                            onClick: new ClickableLink(`vscode-insiders://${link}`),
                        }),
                        Toolbar.ToolbarItems.createBasicButton(lifetime, {
                            label: 'Change settings',
                            icon: Images.Gear(),
                            disabled:
                                row.item.state.toLowerCase() === startingLower ||
                                row.item.state.toLowerCase() === provisioningLower ||
                                row.item.state.toLowerCase() === shuttingDownLower,
                            onClick: () => {
                                this.context.container.openContextPane(
                                    BladeReferences.forBlade('EditCodespaceBlade').createReference({
                                        parameters: {
                                            planId: this.context.parameters.id,
                                            codespaceId: row.id,
                                        },
                                        onClosed: () => {
                                            codespacesGrid.refresh();
                                        },
                                    })
                                );
                            },
                        }),
                    ];
                },
            },
        });
        codespacesGrid.refresh();

        return codespacesGrid;
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
        includeTags: false,
        expanded: ko.observable(true),
        additionalRight: additionaldata,
    });
    return essentials;
}

function createToolbar(
    container: TemplateBlade.Container,
    planId: string,
    codespacesGrid: DataGrid.Contract<Codespace>,
    codespacesManager: HttpCodespacesManager
): Toolbar.Contract {
    const commandRefreshButton = Toolbar.ToolbarItems.createBasicButton(container, {
        label: ClientResources.refresh,
        icon: Images.Refresh(),
        onClick: () => {},
    });

    const deletePlanButton = initializePlanDeleteButton(container);

    const createButton = initializeCreateButton(container, planId, codespacesGrid);

    const deleteCodespaceButton = initializeCodespaceDeleteButton(container, codespacesGrid, codespacesManager);

    const suspendCodespaceButton = initializeSuspendButton(container, codespacesGrid, codespacesManager);

    const feedbackButton = Toolbar.ToolbarItems.createBasicButton(container, {
        label: ClientResources.feedback,
        icon: Images.Feedback(),
        onClick: () => {},
    });

    console.log(codespacesGrid.noData.peek());

    const seperator = Toolbar.ToolbarItems.createSeparator();

    const commandBar = Toolbar.create(container, {
        items: [createButton, suspendCodespaceButton, deleteCodespaceButton, seperator, deletePlanButton, commandRefreshButton, seperator, feedbackButton],
    });
    return commandBar;
}

function initializePlanDeleteButton(
    container: TemplateBlade.Container,
):Toolbar.ToolbarItems.BasicButtonContract {
    return Toolbar.ToolbarItems.createBasicButton(container, {
        label: ClientResources.deletePlan,
        icon: Images.Delete(),
        onClick: () => {
            container.openDialog({
                title: ClientResources.deletePlan,
                content: ClientResources.deleteConfirmation,
                buttons: Dialog.DialogButtons.YesNo,
                telemetryName: 'DeleteMyResource',
                onClosed: (result) => {
                    if (result.button === Dialog.DialogButton.Yes) {
                        /* TODO: call plan delete function here */
                    }
                },
            });
        },
    });
}

function initializeCreateButton(
    container: TemplateBlade.Container,
    planId: string,
    codespacesGrid: DataGrid.Contract<Codespace>
): Toolbar.ToolbarItems.BasicButtonContract {
    return Toolbar.ToolbarItems.createBasicButton(container, {
        label: ClientResources.createCodespace,
        icon: Images.Add(),
        onClick: () => openCreateBlade(container, planId, codespacesGrid),
    });
}

function openCreateBlade(
    container: TemplateBlade.Container,
    planId: string,
    codespacesGrid: DataGrid.Contract<Codespace>
) {
    container.openContextPane(
        BladeReferences.forBlade('CreateCodespaceBlade').createReference({
            parameters: {
                planId: planId
            },
            onClosed: () => {
                codespacesGrid.refresh();
            },
        })
    );
}

function initializeCodespaceDeleteButton(
    container: TemplateBlade.Container,
    codespacesGrid: DataGrid.Contract<Codespace>,
    codespacesManager: HttpCodespacesManager,
):Toolbar.ToolbarItems.BasicButtonContract {
    return Toolbar.ToolbarItems.createBasicButton(container, {
        label: ClientResources.deleteCodespace,
        icon: Images.Delete(),
        onClick: () => {
            Q.all(
                codespacesGrid.selection.selectedItems.peek().map((codespace) =>
                    //TODO: Do we need a confirmation message before taking this action?????
                    codespacesManager.deleteCodespace(codespace.id)
                )
            ).then(() => codespacesGrid.refresh());
        }
    });
}

function initializeSuspendButton(
    container: TemplateBlade.Container,
    codespacesGrid: DataGrid.Contract<Codespace>,
    codespacesManager: HttpCodespacesManager
): Toolbar.ToolbarItems.BasicButtonContract {
    return Toolbar.ToolbarItems.createBasicButton(container, {
        label: ClientResources.suspendCodespace,
        icon: Images.Paused(),
        onClick: () => {
            Q.all(
                codespacesGrid.selection.selectedItems.peek().map((codespace) =>
                    codespacesManager.suspendCodespace(codespace.id)
                )
            ).then(() => codespacesGrid.refresh());
        },
    });
}