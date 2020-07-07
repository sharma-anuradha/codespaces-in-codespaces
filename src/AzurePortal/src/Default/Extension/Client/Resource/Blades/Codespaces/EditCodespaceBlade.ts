import * as ClientResources from 'ClientResources';
import * as TemplateBlade from 'Fx/Composition/TemplateBlade';
import * as Section from 'Fx/Controls/Section';
import * as Button from 'Fx/Controls/Button';
import * as TextBox from 'Fx/Controls/TextBox';
import * as DropDown from 'Fx/Controls/DropDown';
import * as InfoBox from 'Fx/Controls/InfoBox';
import { CodespacesManager } from './CodespacesManager';
import { HttpCodespacesManager } from './HttpCodespacesManager';
import { Sku, Codespace, suspendedLower, shutdownLower } from './CodespaceModels';
import { HttpPlansManager } from '../../HttpPlansManager';
import { trace } from '../../../Shared/Logger';

/**
 * Contract for parameters that will be passed to Keys blade.
 */
export interface EditCodespaceBladeParameters {
    planId: string;
    codespaceId: string;
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
        "<div class='msportalfx-docking-footer msportalfx-padding'>" +
        "<div data-bind='pcControl: okButton' class='.ext-ok-button'></div>" +
        "<div data-bind='pcControl: cancelButton'></div>" +
        '</div>' +
        '</div>',
})
export class EditCodespaceBlade {
    /**
     * The title of Resource Keys blade
     */
    public readonly title = ClientResources.editCodespaceBladeTitle;

    /**
     * The subtitle of Resource Keys blade
     */
    public readonly subtitle = ClientResources.editCodespaceBladeSubtitle;

    /**
     * The context property contains APIs you can call to interact with the shell.
     * It will be populated for you by the framework before your onInitialize() function is called.
     *   https://aka.ms/portalfx/nopdl/context
     */
    public readonly context: TemplateBlade.Context<EditCodespaceBladeParameters>;

    /**
     * The section that hosts the controls.
     */
    public section: Section.Contract;

    //Buttons at the bottom of the form
    public okButton: Button.Contract;
    public cancelButton: Button.Contract;

    private _codespacesManager: CodespacesManager;
    private _currentSkuName: string;
    private _currentSuspendAutoShutdownDelay: number;

    /**
     * Initializes the Blade
     */
    public onInitialize() {
        trace('EditCodespaceBlade', 'Initialize blade');

        const { parameters } = this.context;

        this._codespacesManager = new HttpCodespacesManager(parameters.planId);

        return new HttpPlansManager().fetchPlan(parameters.planId).then((plan) => {
            const locationPromise = this._codespacesManager.fetchLocation(plan.location);
            const codespacePromise = this._codespacesManager.fetchCodespace(parameters.codespaceId);
            return Q.all([locationPromise, codespacePromise]).then(
                ([{ skus, defaultAutoSuspendDelayMinutes }, codespace]) => {
                    return this._initializeSection(skus, defaultAutoSuspendDelayMinutes, codespace);
                }
            );
        });
    }

    /**
     * Initializes the section
     */
    private _initializeSection(
        skus: Sku[],
        autoSuspendDelays: number[],
        codespace: Codespace
    ): void {
        const { container } = this.context;

        this._currentSkuName = codespace.skuName;
        this._currentSuspendAutoShutdownDelay = codespace.autoShutdownDelayMinutes;

        const nameTextBox = TextBox.create(container, {
            label: 'Name',
            infoBalloonContent: 'Codespace Name',
            disabled: true,
            value: codespace.friendlyName,
        });

        const items = skus.map((sku) => ({
            text: sku.displayName,
            value: sku.name,
        }));

        const skuDropDown = DropDown.create(container, {
            label: 'Instance',
            infoBalloonContent: 'SKU',
            items: items,
            value: this._currentSkuName,
        });

        const times = autoSuspendDelays.map((delay) => ({
            text: delay.toString(),
            value: delay,
        }));

        const autoShutdownDelayDropDown = DropDown.create(container, {
            label: 'Auto Shutdown Delay',
            infoBalloonContent: 'Useful info for auto shutdown delay',
            items: times,
            value: this._currentSuspendAutoShutdownDelay,
        });

        this.section = Section.create(container, {
            children: [nameTextBox, skuDropDown, autoShutdownDelayDropDown],
        });

        this.context.form.configureAlertOnClose(ko.observable({ showAlert: false }));

        this.okButton = Button.create(container, {
            text: 'Apply',
            onClick: () => {
                const skuName =
                    skuDropDown.value.peek() !== this._currentSkuName
                        ? skuDropDown.value.peek()
                        : undefined;
                const autoShutdownDelayMinutes =
                    autoShutdownDelayDropDown.value.peek() !== this._currentSuspendAutoShutdownDelay
                        ? autoShutdownDelayDropDown.value.peek()
                        : undefined;

                this._codespacesManager
                    .editCodespace(codespace.id, skuName, autoShutdownDelayMinutes)
                    .then(() => {
                        this.context.container.closeCurrentBlade();
                    })
                    .catch(() => {
                        this.context.container.fail('Failed to edit your codespace');
                    });
            },
            disabled: true,
        });

        if (
            codespace.state.toLowerCase() === suspendedLower ||
            codespace.state.toLowerCase() === shutdownLower
        ) {
            this.okButton.disabled(false);
        } else {
            const infoBox = InfoBox.create(container, {
                style: InfoBox.Style.Info,
                text: 'Suspend the codespace to update settings',
            });

            this.section.children.unshift(infoBox);
        }

        this.cancelButton = Button.create(container, {
            text: 'Cancel',
            onClick: () => {
                this.context.container.closeCurrentBlade();
            },
            style: Button.Style.Secondary,
        });
    }
}
