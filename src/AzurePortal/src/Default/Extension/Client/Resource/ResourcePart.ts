import * as ClientResources from "ClientResources";
import { BladeReferences } from "Fx/Composition";
import * as TemplatePart from "Fx/Composition/TemplatePart";
import { SvgType } from "Fx/Images";
import * as ResourceOverviewBlade from "./Blades/Overview/ResourceOverviewBlade";

/**
 * Resource Part that implements pinned part for Overview blade
 * Learn more about decorator based parts at: https://aka.ms/portalfx/nopdl
 */
@TemplatePart.Decorator({
    htmlTemplate: "<div data-bind='text: title'></div>",
    forAsset: { assetIdParameter: "id", assetType: "MicrosoftCodespacesPlans" },
    galleryMetadata: {
        title: ClientResources.resourcePartTitle,
        category: ClientResources.resourcePartSubtitle,
        thumbnail: { image: SvgType.Favorite },
    },
})
export class ResourcePart {
    /**
     * The title of resource part
     */
    public title = ClientResources.resourcePartTitle;

    /**
     * The subtitle of resource part
     */
    public subtitle = ClientResources.resourcePartSubtitle;

    /**
     * The context property contains APIs you can call to interact with the shell.
     * It will be populated for you by the framework before your onInitialize() function is called.
     *   https://aka.ms/portalfx/nopdl/context
     */
    public context: TemplatePart.Context<ResourceOverviewBlade.Parameters>;

    /**
     * Initialize the part.
     */
    public onInitialize() {
        return Q();
    }

    /**
     * Specify on click action
     */
    public onClick() {
        const { container, parameters } = this.context;
        return container.openBlade(
            BladeReferences.forBlade("ResourceOverviewBlade").createReference({
                parameters,
            })
        );
    }
}
