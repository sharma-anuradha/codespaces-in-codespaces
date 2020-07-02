// Compiled with TypeScript 3.9

// FILE: FxObsolete\Controls\BaseResourceDropDown.d.ts
declare module "FxObsolete/Controls/BaseResourceDropDown" {
    import FxViewModels = MsPortalFx.ViewModels;
    import Forms = FxViewModels.Forms;
    export = Main;
    module Main {
        /**
         * The contract for legacy controls
         * It has a different item structure and a helper to find objects by name
         */
        interface Contract<TValue> {
            /**
             * The available items for the dropdown control
             */
            readonly items: KnockoutObservableBase<any[]>;
            /**
             * Returns the corresponding object in the drop down list for the given name.
             * @param  name The name to match the object.
             * @return The corresponding object that matches the name.
             */
            getObjectByName(name: string): TValue;
        }
        /**
         * The contract for legacy controls
         * It has a different item structure and a helper to find objects by name
         */
        interface Options<TValue> {
            form: Forms.Form.ViewModel<any>;
            /**
             * The edit scope accessor associated with the drop down control.
             */
            accessor: Forms.EditScopeAccessors<TValue>;
            /**
             * A list of validations that should be applied to the form field.
             */
            validations: KnockoutObservableArray<MsPortalFx.ViewModels.FormValidation>;
        }
        interface ResourceGroupOptions<TValue> extends Options<TValue> {
            /**
             * The observable that holds the subscription id used to filter locations.
             */
            subscriptionIdObservable: KnockoutObservableBase<string>;
        }
        interface LocationOptions<TValue> extends Options<TValue> {
            /**
             * The observable that holds the subscription id used to filter locations.
             */
            subscriptionIdObservable: KnockoutObservableBase<string>;
            /**
             * Optional. The observable that holds the list of resource types used to filter locations.
             */
            resourceTypesObservable?: KnockoutObservableBase<string[]>;
        }
    }
}

// FILE: FxObsolete\Controls\LocationDropDown.d.ts
declare module "FxObsolete/Controls/LocationDropDown" {
    import { Location as BaseLocation, Validation as BaseValidation } from "Fx/Controls/BaseResourceDropDown";
    import { Location as ArmLocation } from "Fx/ResourceManagement";
    import { HtmlContent } from "Fx/Controls/ControlsBase";
    import { Contract as LegacyContract, LocationOptions as LegacyOptions } from "FxObsolete/Controls/BaseResourceDropDown";
    export = Main;
    module Main {
        import FxViewModels = MsPortalFx.ViewModels;
        /**
         * The validation type accepted by the dropdown
         */
        type Validation = BaseValidation<Location>;
        /**
         * The contract for the values returned by the location dropdown
         */
        type Location = ArmLocation;
        /**
         * The contract for options to create the location drop down
         */
        type Options<THtmlKeyMap extends StringMap<HtmlContent> = StringMap<HtmlContent>> = BaseLocation.BaseOptions<THtmlKeyMap> & LegacyOptions<Location>;
        /**
         * The contract for the location dropdown
         */
        interface Contract extends BaseLocation.Contract, LegacyContract<Location> {
        }
        /**
         * @deprecated FxObsolete.Controls.Location.create This control is no longer supported. Use Fx/Controls/LocationDropDown instead. http://aka.ms/portalfx/breaking
         */
        function create<THtmlKeyMap extends StringMap<HtmlContent>>(container: FxViewModels.ContainerContract, legacyOptions?: Options<THtmlKeyMap>): Contract;
    }
}

// FILE: FxObsolete\Controls\ResourceGroupDropDown.d.ts
declare module "FxObsolete/Controls/ResourceGroupDropDown" {
    import { ResourceGroup } from "Fx/Controls/BaseResourceDropDown";
    import { Contract as LegacyContract, ResourceGroupOptions as LegacyOptions } from "FxObsolete/Controls/BaseResourceDropDown";
    import { HtmlContent } from "Fx/Controls/ControlsBase";
    export = Main;
    module Main {
        import FxViewModels = MsPortalFx.ViewModels;
        /**
         * The modes possible for the dropdown
         */
        export import Mode = ResourceGroup.Mode;
        /**
         * The mode of the value returned by the control
         */
        export import SelectedMode = ResourceGroup.SelectedMode;
        /**
         * The contract for the values returned by the subscription dropdown
         */
        export import Value = ResourceGroup.Value;
        /**
         * The contract for options to create the subscription drop down
         */
        type Options<THtmlKeyMap extends StringMap<HtmlContent> = StringMap<HtmlContent>> = ResourceGroup.BaseOptions<THtmlKeyMap> & LegacyOptions<Value>;
        /**
         * The contract for the subscription dropdown
         */
        interface Contract extends ResourceGroup.Contract, LegacyContract<ResourceGroup.Value> {
        }
        /**
         * @deprecated FxObsolete.Controls.ResourceGroup.create This control is no longer supported. Use Fx/Controls/ResourceGroupDropDown instead. http://aka.ms/portalfx/breaking
         */
        function create<THtmlKeyMap extends StringMap<HtmlContent>>(container: FxViewModels.ContainerContract, legacyOptions?: Options<THtmlKeyMap>): Contract;
    }
}

// FILE: FxObsolete\Controls\SubscriptionDropDown.d.ts
declare module "FxObsolete/Controls/SubscriptionDropDown" {
    import { Subscription } from "Fx/Controls/BaseResourceDropDown";
    import { Contract as LegacyContract, Options as BaseLegacyOptions } from "FxObsolete/Controls/BaseResourceDropDown";
    import { HtmlContent } from "Fx/Controls/ControlsBase";
    export = Main;
    module Main {
        import FxViewModels = MsPortalFx.ViewModels;
        /**
         * The contract for the values returned by the subscription dropdown
         */
        export import Subscription = MsPortalFx.Azure.Subscription;
        /**
         * The contract for options to create the subscription drop down
         */
        type Options<THtmlKeyMap extends StringMap<HtmlContent> = StringMap<HtmlContent>> = Subscription.BaseOptions<THtmlKeyMap> & BaseLegacyOptions<Subscription>;
        /**
         * The contract for the subscription dropdown
         */
        interface Contract extends Subscription.Contract, LegacyContract<Subscription> {
            /**
             * An observable which holds the string id of the subscription
             */
            subscriptionId: KnockoutObservableBase<string>;
        }
        /**
         * @deprecated FxObsolete.Controls.Subscriptions.create This control is no longer supported. Use Fx/Controls/SubscriptionDropDown instead. http://aka.ms/portalfx/breaking
         */
        function create<THtmlKeyMap extends StringMap<HtmlContent>>(container: FxViewModels.ContainerContract, legacyOptions?: Options<THtmlKeyMap>): Contract;
    }
}
