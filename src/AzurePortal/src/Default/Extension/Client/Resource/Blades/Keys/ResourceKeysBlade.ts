import * as ClientResources from "ClientResources";
import * as TemplateBlade from "Fx/Composition/TemplateBlade";
import * as Section from "Fx/Controls/Section";
import * as Toolbar from "Fx/Controls/Toolbar";
import * as CopyableLabel from "Fx/Controls/CopyableLabel";
//import { Images } from "Fx/Images"
import Images = MsPortalFx.Base.Images;

/**
 * Contract for parameters that will be passed to Keys blade.
 */
export interface ResourceKeysBladeParameters {
    id: string;
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
        "</div>" +
        "</div>",
})
export class ResourceKeysBlade {
    /**
     * The title of Resource Keys blade
     */
    public readonly title = 'Example resource keys title';

    /**
     * The subtitle of Resource Keys blade
     */
    public readonly subtitle = 'Example resource keys subtitle';

    /**
     * The context property contains APIs you can call to interact with the shell.
     * It will be populated for you by the framework before your onInitialize() function is called.
     *   https://aka.ms/portalfx/nopdl/context
     */
    public readonly context: TemplateBlade.Context<ResourceKeysBladeParameters>;

    /**
     * The section that hosts the controls.
     */
    public section: Section.Contract;

    /**
     * View model for the copyableLabel controls.
     */
    private _primaryKeyCopyableViewModel: CopyableLabel.Contract;

    /**
     * View model for the copyableLabel controls
     */
    private _secondaryKeyCopyableViewModel: CopyableLabel.Contract;

    /**
     * Initializes the Blade
     */
    public onInitialize() {
        const { container, parameters } = this.context;
        const id = parameters.id;
        this._initializeSection();
        this._initializeCommandBar();
        return Q();
    }

    /**
     * Initializes the section
     */
    private _initializeSection(): void {
        const { container } = this.context;
        this._primaryKeyCopyableViewModel = CopyableLabel.create(container, {
            label: ClientResources.keyPrimaryKeyLabel,
            readOnly: true,
            value:
                "1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121-1800-3456-0988888821-212121212121",
        });

        this._secondaryKeyCopyableViewModel = CopyableLabel.create(container, {
            label: ClientResources.keySecondaryKeyLabel,
            readOnly: true,
            value: "20320-21546-3230923020932-89586473479",
        });

        this.section = Section.create(container, {
            name: "Keys",
            children: [
                this._primaryKeyCopyableViewModel,
                this._secondaryKeyCopyableViewModel,
            ],
        });
    }

    /**
     * Initializes the command bar
     */
    private _initializeCommandBar(): void {
        const { container } = this.context;
        const primaryKeyButton = Toolbar.ToolbarItems.createBasicButton(
            container,
            {
                label: ClientResources.keyPrimaryKeyLabel,
                icon: Images.Redo(),
                onClick: () => {
                    this._primaryKeyCopyableViewModel.value(
                        "212121212121-1800-3456-0988888821"
                    );
                },
            }
        );

        const secondaryKeyButton = Toolbar.ToolbarItems.createBasicButton(
            container,
            {
                label: ClientResources.keySecondaryKeyLabel,
                icon: Images.Redo(),
                onClick: () => {
                    this._secondaryKeyCopyableViewModel.value(
                        "89586473479-20320-21546-3230923020932"
                    );
                },
            }
        );

        const commandBar = Toolbar.create(container, {
            items: [primaryKeyButton, secondaryKeyButton],
        });
        container.commandBar = commandBar;
    }
}
