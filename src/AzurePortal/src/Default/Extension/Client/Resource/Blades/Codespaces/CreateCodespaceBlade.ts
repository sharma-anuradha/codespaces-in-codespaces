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
import { validateGitUrl, getValidationMessage, validationMessagesKeys } from './gitValidation';
import * as OAuthButton from 'Fx/Controls/OAuthButton';
import { DefaultIndexedDB } from '../../../Shared/indexedDBFS';
import { ajax } from 'Fx/Ajax';
import { githubClientId, codespacesBaseUri } from '../../../Shared/Endpoints';
import { uuid, encryptedGitAccessTokenKey } from '../../../Shared/Constants';
import { normalizeGitUrl } from './gitUrlNormalization';

/**
 * Contract for parameters that will be passed to Keys blade.
 */
export interface CreateCodespaceBladeParameters {
    planId: string;
}

interface githubAccessTokenResponse {
    accessToken: string;
    scope: string;
    tokenType: string;
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
        "<div class='msportalfx-form' data-bind='pcControl: basicsSection'></div>" +
        "<div class='msportalfx-form' data-bind='pcControl: dotFilesSection'></div>" +
        '</div>' +
        "<div class='msportalfx-docking-footer msportalfx-padding'>" +
        "<div data-bind='pcControl: oauthButton' class='.ext-ok-button'></div>" +
        "<div data-bind='pcControl: okButton' class='.ext-ok-button'></div>" +
        "<div data-bind='pcControl: cancelButton'></div>" +
        '</div>' +
        '</div>',
    styleSheets: ['CreateCodespaceBlade.css'],
})
@TemplateBlade.ReturnsData.Decorator()
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
    public readonly context: TemplateBlade.Context<CreateCodespaceBladeParameters> &
        TemplateBlade.ReturnsData.Context<{
            encryptedGitToken: string;
            codespaceId: string;
        }>;

    //The section that contains necessary codespace creation info
    public basicsSection: Section.Contract;

    //The section that contains dotfiles parameters
    public dotFilesSection: Section.Contract;

    //Buttons at the bottom of the form
    public oauthButton: OAuthButton.Contract;
    public okButton: Button.Contract;
    public cancelButton: Button.Contract;

    private _codespacesManager: CodespacesManager;
    private _gitAccessToken: string;
    private _encryptedGitAccessToken: string;
    private _gitAuthRequired: boolean = false;

    /**
     * Initializes the Blade
     */
    public onInitialize() {
        const { parameters } = this.context;
        const planId = parameters.planId;
        this._codespacesManager = new HttpCodespacesManager(planId);

        return new HttpPlansManager().fetchPlan(planId).then((plan) => {
            return this._codespacesManager
                .fetchLocation(plan.location)
                .then(({ skus, defaultAutoSuspendDelayMinutes }) => {
                    return DefaultIndexedDB.getValue(encryptedGitAccessTokenKey).then(
                        async (value) => {
                            if (value) {
                                this._encryptedGitAccessToken = value;

                                const decryptedAuthToken = await ajax<string>({
                                    type: 'POST',
                                    crossDomain: true,
                                    headers: {
                                        'Content-Type': 'application/json',
                                    },
                                    uri: `${codespacesBaseUri}github-auth/decrypt-token`,
                                    data: JSON.stringify({
                                        value,
                                    }),
                                });

                                this._gitAccessToken = decryptedAuthToken;
                            }

                            return this._initializeSection(
                                plan.location,
                                skus,
                                defaultAutoSuspendDelayMinutes
                            );
                        }
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
                new Validations.RegExMatch('^[A-Za-z0-9_()-. ]+$', 'Name is invalid'),
            ],
        });

        const gitUrlTextBox = TextBox.create(container, {
            label: 'Git Url',
            infoBalloonContent: 'Git Url',
            value: '',
            validations: [this._customGitUrlValidation()],
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
        });

        this.basicsSection = Section.create(container, {
            name: 'Create Codespace',
            children: [nameTextBox, gitUrlTextBox, skuDropDown, autoShutdownDelayDropDown],
        });

        //Dotfiles
        const dotFilesRepoTextBox = TextBox.create(container, {
            label: 'Dotfiles repository',
            infoBalloonContent: 'Dotfiles repository',
            value: '',
            validations: [this._customGitUrlValidation()],
        });

        const dotFilesInstallCmdTextBox = TextBox.create(container, {
            label: 'Dotfiles install command',
            infoBalloonContent: 'Dotfiles install command',
            placeHolderText: './install.sh',
        });

        const dotFilesTargetTextBox = TextBox.create(container, {
            label: 'Dotfiles target path',
            infoBalloonContent: 'Dotfiles target files',
            placeHolderText: '~/dotfiles',
        });

        (this.dotFilesSection = Section.create(container, {
            name: 'Dotfiles',
            children: [dotFilesRepoTextBox, dotFilesInstallCmdTextBox, dotFilesTargetTextBox],
        })),
            //Footer
            this.context.form.configureAlertOnClose(ko.observable({ showAlert: false }));

        this.context.form.validationState.subscribe(container, (validationState) => {
            if (this._gitAuthRequired) {
                this.oauthButton.visible(true);
                this.okButton.visible(false);
                this.okButton.disabled(true);
            } else if (validationState === Validations.ValidationState.Valid) {
                this.oauthButton.visible(false);
                this.okButton.visible(true);
                this.okButton.disabled(false);
            } else {
                this.oauthButton.visible(false);
                this.okButton.visible(true);
                this.okButton.disabled(true);
            }
        });

        nameTextBox.validationResults.subscribe(container, () => {
            gitUrlTextBox.triggerValidation();
            dotFilesRepoTextBox.triggerValidation();
        });

        const stateParam1 = uuid();
        const stateParam2 = uuid();
        this.oauthButton = OAuthButton.create(container, {
            onAuthenticationSucceeded: async (codeUrl: string) => {
                // Get params and exchange code for accessToken
                const queryString = codeUrl.substr(codeUrl.indexOf('?'));
                const urlParams = new URLSearchParams(queryString);
                const code = urlParams.get('code');
                const state = urlParams.get('state');
                if (stateParam1 === state) {
                    const authToken = await ajax<githubAccessTokenResponse>({
                        type: 'GET',
                        crossDomain: true,
                        useRawAjax: true,
                        uri: `${codespacesBaseUri}github-auth/azure-portal-access-token?code=${code}&state=${stateParam2}`,
                    });

                    const encryptedAuthToken = await ajax<string>({
                        type: 'POST',
                        crossDomain: true,
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        uri: `${codespacesBaseUri}github-auth/encrypt-token`,
                        data: JSON.stringify({
                            value: authToken.accessToken,
                        }),
                    });

                    DefaultIndexedDB.setValue(encryptedGitAccessTokenKey, encryptedAuthToken);

                    // Revalidate git urls
                    this._encryptedGitAccessToken = encryptedAuthToken;
                    this._gitAccessToken = authToken.accessToken;
                    gitUrlTextBox.triggerValidation();
                    dotFilesRepoTextBox.triggerValidation();
                    this._gitAuthRequired = false;
                } else {
                    this.context.container.fail('State validation failed');
                }
            },
            buttonText: 'Auth',
            requestUrl: `https://github.com/login/oauth/authorize?client_id=${githubClientId}&scope=repo%20workflow&state=${stateParam1}`,
            visible: false,
        });

        this.okButton = Button.create(container, {
            text: 'Create',
            onClick: () => {
                //create Codespace
                const gitUrl = normalizeGitUrl(gitUrlTextBox.value.peek());
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
                        personalization: {
                            dotfilesRepository: normalizeGitUrl(dotFilesRepoTextBox.value.peek()),
                            dotfilesInstallCommand: normalizeOptionalValue(
                                dotFilesInstallCmdTextBox.value.peek()
                            ),
                            dotfilesTargetPath:
                                normalizeOptionalValue(dotFilesTargetTextBox.value.peek()) ||
                                '~/dotfiles',
                        },
                    })
                    .then((codespace) => {
                        this.context.container.closeCurrentBlade({
                            encryptedGitToken: this._encryptedGitAccessToken,
                            codespaceId: codespace.id,
                        });
                    })
                    .catch((e) => {
                        // TODO tasogawa revisit this
                        this.context.container.fail('Failed to create your codespace');
                    });
            },
            disabled: true,
        });

        this.cancelButton = Button.create(container, {
            text: 'Cancel',
            onClick: () => {
                this.context.container.closeCurrentBlade();
            },
            style: Button.Style.Secondary,
        });
    }

    private _customGitUrlValidation() {
        return new Validations.Custom(
            'Invalid Url',
            (url: string): Q.Promise<Validations.ValidationResult> => {
                return Q(
                    validateGitUrl(url, this._gitAccessToken).then((validationMessageKey) => {
                        if (
                            validationMessageKey === validationMessagesKeys.testFailed &&
                            !this._gitAccessToken
                        ) {
                            // Possibly private repo, postpone validation until access token is obtained
                            this._gitAuthRequired = true;

                            return getValidationMessage(validationMessagesKeys.valid, (s) => {
                                return s;
                            });
                        } else {
                            this._gitAuthRequired = false;

                            return getValidationMessage(validationMessageKey, (s) => {
                                return s;
                            });
                        }
                    })
                );
            }
        );
    }
}

function normalizeOptionalValue(value: string): string | undefined {
    if (!value) {
        return undefined;
    }
    value = value.trim();

    return value;
}
