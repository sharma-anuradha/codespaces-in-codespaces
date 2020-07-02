import { Container } from "Fx/Composition/TemplateBlade";
import * as CustomHtml from "Fx/Controls/CustomHtml";

export interface ExternalLink {
    readonly text: string;
    readonly uri: string;
    readonly useButtonDesign?: boolean;
}

export interface OnClickLink {
    readonly text: string;
    readonly onClick: () => void;
}

export interface InfoTile {
    readonly title: ExternalLink | OnClickLink;
    readonly description: string;
    readonly icon: MsPortalFx.Base.Image;
    readonly links: (ExternalLink | OnClickLink)[];
}

export interface Options {
    readonly infoTiles: InfoTile[];
}

export interface Contract {
    readonly control: CustomHtml.Contract;
}

export function create(container: Container, options: Options): Contract {
    return new InfoTilesComponent(container, options);
}

const HtmlTemplate = `
<div class="ext-info-tiles" data-bind="foreach: infoTiles">
    <div class="ext-info-tile">
        <div class="ext-info-tile__icon" data-bind="image: $data.icon"></div>
        <div>
            <!-- ko if: $data.title.uri -->
                <h3 class="ext-info-tile__title">
                    <a class="ext-info-tile__link" data-bind="text: $data.title.text, attr: { href: $data.title.uri }" target="_blank"></a>
                    <span class="ext-info-tile__hyperlink-icon" data-bind="image: $root.linkIcon"></span>
                </h3>
            <!-- /ko -->
            <!-- ko if: $data.title.onClick -->
                <h3 class="ext-info-tile__title msportalfx-text-primary" data-bind="text: $data.title.text, fxclick: $data.title.onClick"></h3>
            <!-- /ko -->
            <p class="ext-info-tile__description">
                <span data-bind="text: $data.description"></span>
                <!-- ko foreach: { data: $data.links, as: '$link' } -->
                    <!-- ko if: $link.uri -->
                        <!-- ko ifnot: $link.useButtonDesign -->
                            <a class="ext-info-tile__link" data-bind="text: $link.text, attr: { href: $link.uri }" target="_blank"></a>
                            <span class="ext-info-tile__hyperlink-icon" data-bind="image: $root.linkIcon"></span>
                        <!-- /ko -->
                    <!-- /ko -->
                <!-- /ko -->
            </p>
            <ul class="msportalfx-removeDefaultListStyle" data-bind="foreach: { data: $data.links, as: '$link' }">
                <!-- ko if: $link.uri -->
                    <li>
                        <!-- ko if: $link.useButtonDesign -->
                            <a class="ext-info-tile__link-button" data-bind="text: $link.text, attr: { href: $link.uri }" target="_blank"></a>
                        <!-- /ko -->
                    </li>
                <!-- /ko -->
                <!-- ko if: $link.onClick -->
                    <li>
                        <button class="ext-info-tile__button msportalfx-text-primary" data-bind="text: $link.text, fxclick: $link.onClick"></button>
                    </li>
                <!-- /ko -->
            </ul>
        </div>
    </div>
</div>
`;

class InfoTilesComponent {
    public readonly control: CustomHtml.Contract;

    constructor(container: Container, options: Options) {
        this.control = CustomHtml.create(container, {
            htmlTemplate: HtmlTemplate,
            innerViewModel: {
                infoTiles: options.infoTiles,
                linkIcon: MsPortalFx.Base.Images.Hyperlink(),
            },
        });
    }
}
