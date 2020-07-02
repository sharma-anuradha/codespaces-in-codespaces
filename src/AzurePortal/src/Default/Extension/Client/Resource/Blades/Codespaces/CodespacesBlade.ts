import * as ClientResources from 'ClientResources';
import * as TemplateBlade from 'Fx/Composition/TemplateBlade';
import * as Section from 'Fx/Controls/Section';
import * as DataGrid from 'Fx/Controls/DataGrid';
import { Codespace, provisioningLower } from './CodespaceModels';
import { getCodespacesConnectUri } from '../../../Shared/Endpoints';
import { CodespacesManager } from './CodespacesManager';
import { HttpCodespacesManager } from './HttpCodespacesManager';
import { BladeReferences, ClickableLink } from 'Fx/Composition';
import * as Toolbar from 'Fx/Controls/Toolbar';
import Images = MsPortalFx.Base.Images;

/**
 * Contract for parameters that will be passed to Keys blade.
 */
export interface CodespacesParameters {
    planId: string;
}

/**
 * Example Key blade used by some extensions to demonstrate defining a Blade
 * displaying data for your resource and handling commands.
 * Learn more about defining Blades with TypeScript decorators at: https://aka.ms/portalfx/nopdl
 */
@TemplateBlade.Decorator({
    htmlTemplate:
        "<div class='msportalfx-docking'>" +
        "<div class='msportalfx-docking-body msportalfx-padding'>" +
        "<div class='msportalfx-form' data-bind='pcControl: section'></div>" +
        '</div>' +
        '</div>',
})
export class CodespacesBlade {
    /**
     * The title of environments blade
     */
    public readonly title = ClientResources.codepsacesBladeTitle;

    /**
     * The subtitle of environments blade
     */
    public readonly subtitle = ClientResources.codespacesBladeSubtitle;

    /**
     * The context property contains APIs you can call to interact with the shell.
     * It will be populated for you by the framework before your onInitialize() function is called.
     *   https://aka.ms/portalfx/nopdl/context
     */
    public readonly context: TemplateBlade.Context<CodespacesParameters>;

    /**
     * The section that hosts the controls.
     */
    public section: Section.Contract;

    private _codespacesManager: CodespacesManager;

    public onInitialize() {
        const { container, parameters } = this.context;
        const planId = parameters.planId;
        this._codespacesManager = new HttpCodespacesManager(planId);

        this.section = Section.create(container);

        const codespacesGrid = this._initializeCodespacesGrid();
        this.section.children.push(codespacesGrid);

        const createButton = this._initializeCreateButton(codespacesGrid, codespacesGrid.refresh);
        const suspendButton = this._initializeSuspendButton(
            codespacesGrid.selection.selectedItems.peek(),
            () => codespacesGrid.refresh()
        );

        const deleteButton = this._initializeDeleteButton(
            codespacesGrid.selection.selectedItems.peek(),
            () => codespacesGrid.refresh()
        );

        container.commandBar = Toolbar.create(container, {
            items: [createButton, suspendButton, deleteButton],
        });

        return Q();
    }

    private _initializeCodespacesGrid(): DataGrid.Contract<Codespace> {
        const dataSource = () =>
            this._codespacesManager.fetchCodespaces().then((codespaces: Codespace[]) => {
                codespaces
                    .filter((codespace) => codespace.state.toLowerCase() === provisioningLower)
                    .forEach(({ id }) =>
                        this._codespacesManager
                            .pollForReadyCodespace(id)
                            .then(() => codespacesGrid.refresh())
                    );

                return codespaces.map((codespace) => ({ id: codespace.id, item: codespace }));
            });

        const columns: DataGrid.ColumnDefinition<Codespace>[] = [
            {
                header: 'Name',
                type: 'UriLink',
                cell: {
                    uriLink: (c) => ({
                        text: c.friendlyName,
                        uri: getCodespacesConnectUri(c.id),
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
                            onClick: () => {
                                // TODO: change settings
                            },
                        }),
                    ];
                },
            },
        });
        codespacesGrid.refresh();

        return codespacesGrid;
    }

    private _initializeCreateButton(
        codespacesGrid: DataGrid.Contract<Codespace>,
        callback: () => Q.Promise<any>
    ): Toolbar.ToolbarItems.BasicButtonContract {
        return Toolbar.ToolbarItems.createBasicButton(this.context.container, {
            label: 'Create New',
            icon: Images.Add(),
            onClick: () => {
                this.context.container.openContextPane(
                    BladeReferences.forBlade('CreateCodespaceBlade').createReference({
                        parameters: {
                            planId: this.context.parameters.planId,
                        },
                        onClosed: () => {
                            codespacesGrid.refresh();
                        },
                    })
                );
            },
        });
    }

    private _initializeSuspendButton(
        codespaces: Codespace[],
        callback: () => Q.Promise<any>
    ): Toolbar.ToolbarItems.BasicButtonContract {
        return Toolbar.ToolbarItems.createBasicButton(this.context.container, {
            label: 'Suspend selected',
            icon: Images.Paused(),
            onClick: () => {
                Q.all(
                    codespaces.map((codespace) =>
                        this._codespacesManager.suspendCodespace(codespace.id)
                    )
                ).then(() => callback());
            },
        });
    }

    private _initializeDeleteButton(
        codespaces: Codespace[],
        callback: () => Q.Promise<any>
    ): Toolbar.ToolbarItems.BasicButtonContract {
        return Toolbar.ToolbarItems.createBasicButton(this.context.container, {
            label: 'Delete selected',
            icon: Images.Delete(),
            onClick: () => {
                Q.all(
                    codespaces.map((codespace) =>
                        this._codespacesManager.deleteCodespace(codespace.id)
                    )
                ).then(() => callback());
            },
        });
    }
}
