import ClientResources = require("ClientResources");
import * as TemplateBlade from "Fx/Composition/TemplateBlade";
import * as TabControl from "Fx/Controls/TabControl";
import * as Section from "Fx/Controls/Section";
import * as TextBlock from "Fx/Controls/TextBlock";
import * as TextBox from "Fx/Controls/TextBox";
import * as Validations from "Fx/Controls/Validations";
import * as Button from "Fx/Controls/Button";
import * as SubscriptionDropDown from "Fx/Controls/SubscriptionDropDown";
import * as Summary from "Fx/Controls/Summary";
import * as ResourceGroupDropDown from "Fx/Controls/ResourceGroupDropDown";
import * as TagsByResource from "Fx/Controls/TagsByResource";
import * as LocationDropDown from "Fx/Controls/LocationDropDown";
import { ajax } from "Fx/Ajax";

@TemplateBlade.Decorator({
    htmlTemplate: `CreateBlade.html`,
})
@TemplateBlade.DoesProvisioning.Decorator()
export class CreateBlade {
    public context: TemplateBlade.Context<void> &
        TemplateBlade.DoesProvisioning.Context;

    public title: string;
    public subtitle: string;
    public tabs: TabControl.Contract;
    public createButton: Button.Contract;
    public previousButton: Button.Contract;
    public nextButton: Button.Contract;

    public onInitialize() {
        // Initializing the blade with a title and subtitle
        this.title = ClientResources.createBladeTitle;
        this.subtitle = ClientResources.createBladeSubtitle;

        // #region BasicsSection

        // #region Introduction

        const introductionSection = Section.create(this.context.container, {
            children: [
                {
                    htmlTemplate:
                        `<span data-bind="text: descText"></span>` +
                        `<a class="msportalfx-ext-link" href="#" target="_blank" data-bind='attr: { href: linkHref }, text: learnMore'></a>`,
                    viewModel: {
                        linkHref: ClientResources.createBladeIntroductionLink,
                        descText: ClientResources.createBladeIntroduction,
                        learnMore: ClientResources.learnMore,
                    },
                },
            ],
        });

        // #endregion

        // #region ProjectDetails

        const projectDetailsDescription = TextBlock.create(
            this.context.container,
            {
                text: ClientResources.projectDetailsDescription,
            }
        );

        //config#subscriptionDropDown
        const subscriptionDropDown = SubscriptionDropDown.create(
            this.context.container,
            {
                initialSubscriptionId: this.context.provisioning.initialValues
                    .subscriptionIds,
                validations: [new Validations.Required()],
            }
        );

        const subscriptionId = ko.pureComputed(
            () =>
                subscriptionDropDown.value() &&
                subscriptionDropDown.value().subscriptionId
        );
        //config#subscriptionDropDown

        //config#resourceGroupDropDown
        const resourceGroupDropDown = ResourceGroupDropDown.create(
            this.context.container,
            {
                subscriptionId: subscriptionId,
                infoBalloonContent:
                    ClientResources.resourceGroupDropDownInfoBalloon,
                nested: true,
                validations: [new Validations.Required()],
            }
        );
        //config#resourceGroupDropDown

        const projectDetailsSection = Section.create(this.context.container, {
            name: ClientResources.projectDetails,
            smartAlignLabel: true,
            children: [
                projectDetailsDescription,
                subscriptionDropDown,
                resourceGroupDropDown,
            ],
        });

        // #endregion

        // #region VS Codespace plan details

        //config#locationDropDown
        const locationDropDown = LocationDropDown.create(
            this.context.container,
            {
                initialLocationName: ko.observableArray<string>(),
                subscriptionId: subscriptionId,
                validations: [new Validations.Required()],
                resourceTypes: ["Microsoft.Codespaces/plans"],
            }
        );
        //config#locationDropDown

        const nameTextBox = TextBox.create(this.context.container, {
            label: ClientResources.name,
            showValidationsAsPopup: true,
            validations: [new Validations.Required()],
        });

        const instanceDetailsSection = Section.create(this.context.container, {
            name: ClientResources.instanceDetails,
            smartAlignLabel: true,
            children: [nameTextBox, locationDropDown],
        });

        // #endregion

        const basicsSection = Section.create(this.context.container, {
            name: ClientResources.basics,
            smartAlignLabel: true,
            children: [
                introductionSection,
                projectDetailsSection,
                instanceDetailsSection,
            ],
        });

        // #endregion

        // #region TagsSection

        const tagsResources: TagsByResource.TargetItem[] = [
            {
                id: "1",
                displayName: ClientResources.CodespacesNames.Plans.singular,
                count: 1,
            },
        ];

        const tags = TagsByResource.create(this.context.container, {
            resources: tagsResources,
        });

        const tagsSection = Section.create(this.context.container, {
            name: ClientResources.tags,
            smartAlignLabel: true,
            children: [tags],
        });

        // #endregion

        // #region ReviewSection

        // #region SummaryDetails

        const summary = Summary.create(this.context.container, {
            children: [
                {
                    name: ClientResources.basics,
                    children: [
                        {
                            label: ClientResources.subscription,
                            value: ko.pureComputed(() => {
                                return (
                                    subscriptionDropDown.value() &&
                                    subscriptionDropDown.value().displayName
                                );
                            }),
                        },
                        {
                            label: ClientResources.resourceGroup,
                            value: ko.pureComputed(() => {
                                return (
                                    resourceGroupDropDown.value() &&
                                    resourceGroupDropDown.value().value.name
                                );
                            }),
                        },
                        {
                            label: ClientResources.name,
                            value: ko.pureComputed(() => {
                                return nameTextBox.value();
                            }),
                        },
                    ],
                },
            ],
        });

        // #endregion

        const reviewSection = Section.create(this.context.container, {
            name: ClientResources.reviewAndCreate,
            smartAlignLabel: true,
            children: [summary],
        });

        // #endregion

        this.tabs = TabControl.create(this.context.container, {
            tabs: [basicsSection, tagsSection, reviewSection],
            cssClass: "msportalfx-create-tab",
            showRequiredStatusOnTabs: false,
            showValidationStatusOnTabs: true,
            smartAlignLabel: true,
        });

        this.createButton = Button.create(this.context.container, {
            text: ClientResources.reviewAndCreate,
            onClick: () => {
                if (this.tabs.activeTab() !== reviewSection) {
                    this.tabs.activeTab(reviewSection);
                } else {
                    const resourceGroupLocation =
                        resourceGroupDropDown.value().mode ===
                        ResourceGroupDropDown.Mode.CreateNew
                            ? locationDropDown.value().name
                            : resourceGroupDropDown.value().value.location;
                    const name = nameTextBox.value();
                    const location = locationDropDown.value().name;

                    return this.context.provisioning.deployTemplate(
                        this._supplyTemplateDeploymentOptions(
                            subscriptionDropDown.value().subscriptionId,
                            resourceGroupDropDown.value().value.name,
                            resourceGroupLocation,
                            name,
                            location,
                            tags.resourceTagsAsMap(tagsResources[0])
                        )
                    );
                }
            },
        });

        // #region NavigationLogic

        this.previousButton = Button.create(this.context.container, {
            text: ClientResources.previous,
            style: Button.Style.Secondary,
            onClick: () => {
                if (this.tabs.activeTab() === tagsSection) {
                    this.tabs.activeTab(basicsSection);
                } else if (this.tabs.activeTab() === reviewSection) {
                    this.tabs.activeTab(tagsSection);
                }
            },
            disabled: ko.pureComputed(() => {
                return this.tabs.activeTab() === basicsSection ? true : false;
            }),
        });

        this.nextButton = Button.create(this.context.container, {
            text: ClientResources.next,
            style: Button.Style.Secondary,
            onClick: () => {
                if (this.tabs.activeTab() === basicsSection) {
                    this.tabs.activeTab(tagsSection);
                } else if (this.tabs.activeTab() === tagsSection) {
                    this.tabs.activeTab(reviewSection);
                }
            },
            disabled: ko.pureComputed(() => {
                return this.tabs.activeTab() === reviewSection ? true : false;
            }),
        });

        this.tabs.activeTab.subscribe(this.context.container, (tab) => {
            if (tab === basicsSection) {
                this.createButton.text(ClientResources.reviewAndCreate);
                this.nextButton.text(
                    ClientResources.next + ": " + ClientResources.tags + " >"
                );
            } else if (tab === tagsSection) {
                this.createButton.text(ClientResources.reviewAndCreate);
                this.nextButton.text(
                    ClientResources.next +
                        ": " +
                        ClientResources.reviewAndCreate +
                        " >"
                );
            } else {
                this.createButton.text(ClientResources.create);
                this.nextButton.text(ClientResources.next);
            }
        });

        // #endregion

        // #region ValidationLogic

        this.tabs.activeTab.subscribe(this.context.container, (tab) => {
            this.context.container.statusBar(undefined);
            if (tab === reviewSection) {
                this.context.form
                    .triggerValidation(false, true)
                    .then((isValid) => {
                        if (isValid) {
                            this.context.container.statusBar({
                                text: ClientResources.createValidationSuccess,
                                state: TemplateBlade.ContentState.Complete,
                            });
                            this.createButton.disabled(false);
                        } else {
                            this.context.container.statusBar({
                                text: ClientResources.createValidationError,
                                state: TemplateBlade.ContentState.Error,
                            });
                            this.createButton.disabled(true);
                        }
                    });
            } else {
                this.createButton.disabled(false);
            }
        });

        // #endregion

        return Q();
    }

    // #region ProvisioningLogic

    private _getTemplateJson(): string {
        return JSON.stringify({
            $schema:
                // "http://schema.management.azure.com/schemas/2019-08-01/deploymentTemplate.json#",
                "http://schema.management.azure.com/schemas/2019-08-01/deploymentTemplate.json#",
            contentVersion: "1.0.0.0",
            parameters: {
                name: {
                    type: "string",
                },
                location: {
                    type: "string",
                },
                tags: {
                    type: "Object",
                },
            },
            resources: [
                {
                    apiVersion: "2020-05-26",
                    //apiVersion: "2019-07-01-alpha",
                    name: "[parameters('name')]",
                    location: "[parameters('location')]",
                    type: "Microsoft.Codespaces/plans",
                    tags: "[parameters('tags')]",
                },
            ],
        });
    }

    private _supplyTemplateDeploymentOptions(
        subscriptionId: string,
        resourceGroupName: string,
        resourceGroupLocation: string,
        name: string,
        location: string,
        tags: ReadonlyStringMap<string>
    ): TemplateBlade.DoesProvisioning.DeployTemplateOptions {
        const galleryCreateOptions = this.context.provisioning.marketplaceItem;
        const resourceIdFormattedString = `/subscriptions/${subscriptionId}/resourcegroups/${resourceGroupName}/providers/Microsoft.Codespaces/plans/${name}`;
        const deferred = Q.defer<
            MsPortalFx.Azure.ResourceManager.TemplateDeploymentOptions
        >();

        const parameters = {
            name: name,
            location: location,
            tags: tags,
        };

        return {
            subscriptionId: subscriptionId,
            resourceGroupName: resourceGroupName,
            resourceGroupLocation: resourceGroupLocation,
            parameters: parameters,
            deploymentName: galleryCreateOptions.deploymentName + Date.now(),
            resourceProviders: ["Microsoft.Codespaces"],
            resourceId: resourceIdFormattedString,
            templateJson: this._getTemplateJson(),
        };
    }

    // #endregion
}
