import * as ClientResources from 'ClientResources';
import { BladeReferences, PartReferences, ClickableLink, BladeClosedReason } from 'Fx/Composition';
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
import { Links, ResourceMenuBladeIds, encryptedGitAccessTokenKey } from '../../../Shared/Constants';
import { trace } from '../../../Shared/Logger';
import * as InfoTilesComponent from './InfoTilesComponent/InfoTilesComponent';
import * as Toolbar from 'Fx/Controls/Toolbar';

import Images = MsPortalFx.Base.Images;
import { HttpPlansManager } from 'Resource/HttpPlansManager';
import { HttpCodespacesManager } from 'Resource/Blades/Codespaces/HttpCodespacesManager';
import {
    Codespace,
    provisioningLower,
    startingLower,
    shuttingDownLower,
} from 'Resource/Blades/Codespaces/CodespaceModels';
import { CodespacesManager } from '../Codespaces/CodespacesManager';
import { PlansManager } from 'Resource/PlansManager';
import { DefaultIndexedDB } from '../../../Shared/indexedDBFS';

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
        '</div>' +
        "<div data-bind='visible: hasCodespaces'>" +
        "<div class='msportalfx-form' data-bind='pcControl: codespaceGridSection'></div>" +
        '</div>' +
        '</div>',
    styleSheets: ['./ResourceOverviewBlade.css', './InfoTilesComponent/InfoTilesComponent.css'],
})
@TemplateBlade.Pinnable.Decorator()
export class ResourceOverviewBlade {
    private _codespacesManager: CodespacesManager;
    private _plansManager: PlansManager;
    private _codespacesCount = ko.observable<string>('0');
    private _encryptedGitAccessToken: string;
    public hasCodespaces = ko.observable(true);
    public hasNoCodespaces = ko.observable(false);
    public _buttonsDisabled = ko.observable(true);

    public infoTilesComponent: InfoTilesComponent.Contract;

    public codespaceGridSection: Section.Contract;

    /**
     * The title of resource overview blade
     */
    public readonly title = ko.observable<string>(ClientResources.resourceOverviewBladeTitle);

    /**
     * The subtitle of resource overview blade
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

        const { parameters } = this.context;
        const id = parameters.id;
        this._codespacesManager = new HttpCodespacesManager(id);
        this._plansManager = new HttpPlansManager();

        this.essentialsViewModel(
            createEssentials(this.context.container, id, this._additionalEssentialData())
        );

        this.codespaceGridSection = Section.create(this.context.container);

        return DefaultIndexedDB.getValue(encryptedGitAccessTokenKey).then((value) => {
            this._encryptedGitAccessToken = value;

            const codespacesGrid = this._initializeCodespacesGrid();
            this.codespaceGridSection.children.push(codespacesGrid);

            // InfoTilesComponent
            const OpenCodespaceTile: InfoTilesComponent.InfoTile = this._createOpenCodespaceTile(
                this.context.container,
                id,
                codespacesGrid
            );
            this.infoTilesComponent = InfoTilesComponent.create(this.context.container, {
                infoTiles: [OpenCodespaceTile],
            });

            return this._plansManager.fetchPlan(this.context.parameters.id).then((result) => {
                this.title(result.name);
                this.context.container.commandBar = this._createToolbar(
                    this.context.container,
                    id,
                    result.name,
                    codespacesGrid,
                    this._buttonsDisabled
                );
                this.context.container.revealContent();
            });
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
                    this._openCreateBlade(container, planId, codespacesGrid);
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
                        this._openCreateBlade(container, planId, codespacesGrid);
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
                        this._codespacesManager.pollTransitioningCodespace(id).then(() => {
                            codespacesGrid.refresh();
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
                        bladeReference: BladeReferences.forBlade(
                            'Connect.ReactView'
                        ).createReference({
                            parameters: {
                                planId: this.context.parameters.id,
                                codespaceId: c.id,
                                codespacesEndpoint: MsPortalFx.getEnvironmentValue(
                                    'codespacesEndpoint'
                                ),
                                armApiVersion: MsPortalFx.getEnvironmentValue('armApiVersion'),
                                encryptedGitToken: this._encryptedGitAccessToken,
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

    private _createToolbar(
        container: TemplateBlade.Container,
        planId: string,
        planName: string,
        codespacesGrid: DataGrid.Contract<Codespace>,
        isDisabled: KnockoutObservable<boolean>
    ): Toolbar.Contract {
        const commandRefreshButton = Toolbar.ToolbarItems.createBasicButton(container, {
            label: ClientResources.refresh,
            icon: Images.Refresh(),
            onClick: () => {},
        });

        const deletePlanButton = this._initializePlanDeleteButton(
            container,
            codespacesGrid,
            planName,
            planId
        );

        const createButton = this._initializeCreateButton(container, planId, codespacesGrid);

        const deleteCodespaceButton = this._initializeCodespaceDeleteButton(
            container,
            codespacesGrid,
            isDisabled
        );
        const suspendCodespaceButton = this._initializeSuspendButton(
            container,
            codespacesGrid,
            isDisabled
        );

        const feedbackButton = Toolbar.ToolbarItems.createBasicButton(container, {
            label: ClientResources.feedback,
            icon: Images.Feedback(),
            onClick: () => {},
        });

        codespacesGrid.selection.selectedItems.subscribe(container, (items) => {
            isDisabled(items.length === 0);
        });

        const commandBar = Toolbar.create(container, {
            items: [
                commandRefreshButton,
                deletePlanButton,
                createButton,
                deleteCodespaceButton,
                suspendCodespaceButton,
                feedbackButton,
            ],
        });
        return commandBar;
    }

    private _initializePlanDeleteButton(
        container: TemplateBlade.Container,
        codespacesGrid: DataGrid.Contract<Codespace>,
        planName: string,
        planId: string
    ): Toolbar.ToolbarItems.BasicButtonContract {
        return Toolbar.ToolbarItems.createBasicButton(container, {
            label: ClientResources.deletePlan,
            icon: Images.Delete(),
            onClick: () => {
                container.openDialog({
                    title: ClientResources.deletePlan,
                    content: ClientResources.deletePlanConfirmation.format(
                        planName,
                        codespacesGrid.rows.peek().length
                    ),
                    buttons: Dialog.DialogButtons.YesNo,
                    telemetryName: 'DeleteMyPlanResource',
                    onClosed: (result) => {
                        if (result.button === Dialog.DialogButton.Yes) {
                            return this._plansManager.deletePlan(planId).then(() => {
                                container.closeCurrentBlade();
                            });
                        }
                    },
                });
            },
        });
    }

    private _initializeCreateButton(
        container: TemplateBlade.Container,
        planId: string,
        codespacesGrid: DataGrid.Contract<Codespace>
    ): Toolbar.ToolbarItems.BasicButtonContract {
        return Toolbar.ToolbarItems.createBasicButton(container, {
            label: ClientResources.createCodespace,
            icon: Images.Add(),
            onClick: () => this._openCreateBlade(container, planId, codespacesGrid),
        });
    }

    private _openCreateBlade(
        container: TemplateBlade.Container,
        planId: string,
        codespacesGrid: DataGrid.Contract<Codespace>
    ) {
        container.openContextPane(
            BladeReferences.forBlade('CreateCodespaceBlade').createReference({
                parameters: {
                    planId: planId,
                },
                onClosed: async (reason, data) => {
                    codespacesGrid.refresh();

                    if (reason === BladeClosedReason.ChildClosedSelf) {
                        if (data) {
                            const codespaceId = data.codespaceId;
                            const encryptedToken = data.encryptedGitToken;
                            this._encryptedGitAccessToken = encryptedToken;

                            container.openBlade(
                                BladeReferences.forBlade('Connect.ReactView').createReference({
                                    parameters: {
                                        planId: planId,
                                        codespaceId: codespaceId,
                                        codespacesEndpoint: MsPortalFx.getEnvironmentValue(
                                            'codespacesEndpoint'
                                        ),
                                        armApiVersion: MsPortalFx.getEnvironmentValue(
                                            'armApiVersion'
                                        ),
                                        encryptedGitToken: this._encryptedGitAccessToken,
                                    },
                                    onClosed: () => {
                                        codespacesGrid.refresh();
                                    },
                                })
                            );
                        }
                    }
                },
            })
        );
    }

    private _initializeCodespaceDeleteButton(
        container: TemplateBlade.Container,
        codespacesGrid: DataGrid.Contract<Codespace>,
        isDisabled: KnockoutObservable<boolean>
    ): Toolbar.ToolbarItems.BasicButtonContract {
        return Toolbar.ToolbarItems.createBasicButton(container, {
            label: ClientResources.deleteCodespace,
            icon: Images.Delete(),
            disabled: isDisabled,
            onClick: () => {
                container.openDialog({
                    title: ClientResources.deleteCodespace,
                    content: ClientResources.deleteCodespaceConfirmation.format(
                        this._getSelectedCodespaceNames(codespacesGrid)
                    ),
                    buttons: Dialog.DialogButtons.YesNo,
                    telemetryName: 'DeleteMyCodespace',
                    onClosed: (result) => {
                        if (result.button === Dialog.DialogButton.Yes) {
                            Q.all(
                                codespacesGrid.selection.selectedItems
                                    .peek()
                                    .map((codespace) =>
                                        this._codespacesManager.deleteCodespace(codespace.id)
                                    )
                            ).then(() => codespacesGrid.refresh());
                        }
                    },
                });
            },
        });
    }

    private _initializeSuspendButton(
        container: TemplateBlade.Container,
        codespacesGrid: DataGrid.Contract<Codespace>,
        isDisabled: KnockoutObservable<boolean>
    ): Toolbar.ToolbarItems.BasicButtonContract {
        return Toolbar.ToolbarItems.createBasicButton(container, {
            label: 'Suspend selected',
            icon: Images.Paused(),
            disabled: isDisabled,
            onClick: () => {
                Q.all(
                    codespacesGrid.selection.selectedItems
                        .peek()
                        .map((codespace) => this._codespacesManager.suspendCodespace(codespace.id))
                ).then(() => codespacesGrid.refresh());
            },
        });
    }

    private _getSelectedCodespaceNames(codespacesGrid: DataGrid.Contract<Codespace>): string {
        return codespacesGrid.selection.selectedItems
            .peek()
            .map((codespace) => {
                return codespace.friendlyName;
            })
            .join(', ');
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
