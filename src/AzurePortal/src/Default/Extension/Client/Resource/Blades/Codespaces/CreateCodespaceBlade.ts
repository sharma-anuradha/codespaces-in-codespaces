import * as ClientResources from 'ClientResources';
import * as TemplateBlade from 'Fx/Composition/TemplateBlade';
import * as Section from 'Fx/Controls/Section';
import * as Button from 'Fx/Controls/Button';
import * as TextBox from 'Fx/Controls/TextBox';
import * as DropDown from 'Fx/Controls/DropDown';
import * as Validations from 'Fx/Controls/Validations';
import { CodespacesManager } from './CodespacesManager';
import { HttpCodespacesManager } from './HttpCodespacesManager';
import { Sku, Seed } from './CodespaceModels';
import { HttpPlansManager } from '../../HttpPlansManager';
import { trace } from '../../../Shared/Logger';
import { validateGitUrl, getValidationMessage } from './gitValidation';

/**
 * Contract for parameters that will be passed to Keys blade.
 */
export interface CreateCodespaceBladeParameters {
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
        "<div class='msportalfx-docking-footer msportalfx-padding'>" +
        "<div data-bind='pcControl: okButton' class='.ext-ok-button'></div>" +
        "<div data-bind='pcControl: cancelButton'></div>" +
        '</div>' +
        '</div>',
    styleSheets: ['CreateCodespaceBlade.css'],
})
export class CreateCodespaceBlade {
    /**
     * The title of Resource Keys blade
     */
    public readonly title = ClientResources.createCodespaceBladeTitle;

    /**
     * The subtitle of Resource Keys blade
     */
    public readonly subtitle = ClientResources.createCodespaceBladeSubtitle;

    /**
     * The context property contains APIs you can call to interact with the shell.
     * It will be populated for you by the framework before your onInitialize() function is called.
     *   https://aka.ms/portalfx/nopdl/context
     */
    public readonly context: TemplateBlade.Context<CreateCodespaceBladeParameters>;

    /**
     * The section that hosts the controls.
     */
    public section: Section.Contract;

    //Buttons at the bottom of the form
    public okButton: Button.Contract;
    public cancelButton: Button.Contract;

    private _codespacesManager: CodespacesManager;

    /**
     * Initializes the Blade
     */
    public onInitialize() {
        trace('CreateCodespacesBlade', 'Initialize blade');

        const { parameters } = this.context;
        const planId = parameters.planId;
        this._codespacesManager = new HttpCodespacesManager(planId);

        return new HttpPlansManager().fetchPlan(planId).then((plan) => {
            return this._codespacesManager
                .fetchLocation(plan.location)
                .then(({ skus, defaultAutoSuspendDelayMinutes }) => {
                    return this._initializeSection(
                        plan.location,
                        skus,
                        defaultAutoSuspendDelayMinutes
                    );
                });
        });
    }

    /**
     * Initializes the section
     */
    private _initializeSection(location: string, skus: Sku[], autoSuspendDelays: number[]): void {
        const { container } = this.context;

        const nameTextBox = TextBox.create(container, {
            label: 'Name',
            infoBalloonContent: 'Codespace Name',
            validations: [
                new Validations.Required('Name is required'),
                new Validations.MaxLength(90, 'Name is too long'),
                new Validations.RegExMatch('^[A-Za-z1-9_()-. ]+$', 'Name is invalid'),
            ],
        });

        const gitUrlTextBox = TextBox.create(container, {
            label: 'Git Url',
            infoBalloonContent: 'Git Url',
            value: '',
            validations: [
                new Validations.Custom(
                    'Invalid Url',
                    (url: string): Q.Promise<Validations.ValidationResult> => {
                        return Q(
                            validateGitUrl(url).then((validationMessageKey) => {
                                return getValidationMessage(validationMessageKey, (s) => {
                                    return s;
                                });
                            })
                        );
                    }
                ),
            ],
        });

        const items = skus.map((sku) => ({
            text: sku.displayName,
            value: sku.name,
        }));

        const skuDropDown = DropDown.create(container, {
            label: 'Instance',
            infoBalloonContent: 'SKU',
            items: items,
            value: skus[0].name,
            ///validations: [new Validations.Required],
        });

        const times = autoSuspendDelays.map((delay) => ({
            text: delay.toString(),
            value: delay,
        }));

        const autoShutdownDelayDropDown = DropDown.create(container, {
            label: 'Auto Shutdown Delay',
            infoBalloonContent: 'Useful info for auto shutdown delay',
            items: times,
            value: times[1].value,
            //validations: [new Validations.Required],
        });

        this.section = Section.create(container, {
            name: 'Create Codespace',
            children: [nameTextBox, gitUrlTextBox, skuDropDown, autoShutdownDelayDropDown],
        });

        this.context.form.configureAlertOnClose(ko.observable({ showAlert: false }));

        this.okButton = Button.create(container, {
            text: 'Create',
            onClick: () => {
                //create Codespace
                const gitUrl = gitUrlTextBox.value.peek();
                const seed: Seed = gitUrl
                    ? {
                          type: 'Git',
                          moniker: gitUrl,
                      }
                    : { type: '' };

                this._codespacesManager
                    .createCodespace({
                        friendlyName: nameTextBox.value.peek(),
                        skuName: skuDropDown.value.peek(),
                        seed,
                        autoShutdownDelayMinutes: autoShutdownDelayDropDown.value.peek(),
                        location,
                    })
                    .then(() => {
                        this.context.container.closeCurrentBlade();
                    })
                    .catch((e) => {
                        // TODO tasogawa revisit this
                        this.context.container.fail('Failed to create your codespace');
                    });
            },
            disabled: true,
        });

        this.context.form.validationState.subscribe(container, (validationState) => {
            if (validationState === Validations.ValidationState.Valid) {
                this.okButton.disabled(false);
            }
        });

        nameTextBox.validationResults.subscribe(container, () => {
            gitUrlTextBox.triggerValidation();
        });

        this.cancelButton = Button.create(container, {
            text: 'Cancel',
            onClick: () => {
                this.context.container.closeCurrentBlade();
            },
            style: Button.Style.Secondary,
        });
    }
}
