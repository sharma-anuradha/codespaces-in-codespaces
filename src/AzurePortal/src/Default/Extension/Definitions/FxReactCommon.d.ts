declare namespace Common {
    interface StringMap<T> {
        [key: string]: T;
    }

    export namespace ResourceManagement {
        /**
         * Data contract for a single location.
         */
        export type Location = {
            /**
             * The display name of the location.
             */
            displayName: string;

            /**
             * The fully qualified ID of the location.
             */
            id?: string;

            /**
             * The normalized name of the location.
             */
            name: string;

            /**
             * The display name of the location and its region.
             */
            regionalDisplayName: string;

            /**
             * Location metadata information
             */
            metadata: {
                /**
                 * The geography group of the location.
                 */
                geographyGroup?: string;

                /**
                 * The latitude of the location.
                 */
                latitude?: number;

                /**
                 * The longitude of the location.
                 */
                longitude?: number;

                /**
                 * The physical location of the location.
                 */
                physicalLocation?: string;

                /**
                 * The region category of the location.
                 */
                regionCategory: keyof typeof RegionSegment;

                /**
                 * The region type of the location.
                 */
                regionType: "Manifest" | "Physical" | "Logical";

                /**
                 * The paired region of the location.
                 */
                pairedRegion?: {
                    /**
                     * The id of the paired location.
                     */
                    id: string;

                    /**
                     * The normalized name of the paired location.
                     */
                    name: string;
                }[];
            };
        }

        /**
         * The enum for which recommended group a location should appear in
         */
        export const enum RegionSegment {
            /**
             * Service Provided
             */
            ServiceProvided = "ServiceProvided",
            /**
             * The first group and largest type of locations with the most resource types supported.
             */
            Recommended = "Recommended",
            /**
             * Other locations including RP specific locations
             */
            Other = "Other",
        }
    }

    export namespace Ajax {
        /**
         * These interfaces are a way of creating a maintainable or string type
         * in combination with "keyof". This produces intellisense for apis.
         * e.g. "GET" | "HEAD" | "POST" | "PUT" | "DELETE"
         */
        interface BatchHttpMethods {
            GET: void;
            HEAD: void;
            POST: void;
            PUT: void;
            DELETE: void;
            PATCH: void;
        }
        /**
         * Http methods for batch ajax calls
         */
        type BatchHttpMethod = keyof BatchHttpMethods;
        /**
         * The request options.
         */
        const enum RequestOptions {
            /**
             * Default behavior.
             *    - Defaults to foreground request
             *    - Calls are batched to ARM every 100 ms
             *    - Any ServerTimeout (503) failures for foreground GET requests
             *      are automatically retried by calling the API directly wihtout batch
             *    - Responses are not cached
             */
            None = 0,
            /**
             * Make the batch call on the next tick.
             * DebounceNextTick takes precedence over Debounce100Ms.
             */
            DebounceNextTick = 1,
            /**
             * Include the request in a batch call that is made after a 100ms delay.
             * Debounce100Ms takes precedence over DebounceOneMinute
             */
            Debounce100ms = 2,
            /**
             * Sets this request to run in the background.
             * Background requests are batched every 60 seconds.
             */
            DebounceOneMinute = 4,
            /**
             * Forces a retry for any failure returned (statusCode >= 400) regardless of the HTTP method.
             */
            RetryForce = 8,
            /**
             * Skips the default retry.
             * SkipRetry takes precedence over ForceRetry if both are set.
             */
            RetrySkip = 16,
            /**
             * Caches the response for GET requests for 10 seconds.
             */
            ResponseCacheEnabled = 32,
            /**
             * Skips caching the response for GET requests.
             */
            ResponseCacheSkip = 64,
            /**
             * Skips retry when a forbidden gateway error is received.
             */
            RetrySkipOnForbidden = 128,
        }
        /**
         * Endpoints used by most extensions.
         */
        type Endpoints = {
            /**
             * The ARM/CSM endpoint with a trailing slash.
             */
            readonly arm: string;
            /**
             * The absolute endpoint of the ARM/CSM batch endpoint with the API version included.
             */
            readonly armBatch: string;
            /**
             * The Graph endpoint with a trailing slash.
             */
            readonly graph: string;
            /**
             * The Microsoft Graph endpoint with a trailing slash.
             */
            readonly msGraph: string;
        };
        /**
         * The settings for the batch call.
         */
        type BatchSettings = {
            /**
             * The request options.
             */
            options?: RequestOptions | number;
            /**
             * The telemetry header to set.
             */
            setTelemetryHeader?: string;
            /**
             * The http method to use. Defaults to GET.
             */
            type?: BatchHttpMethod;
            /**
             * The URI to call.
             */
            uri: string;
            /**
             * Optional content to set for the reqeusts.
             */
            content?: any;
        };
        /**
         * The contract for the batch settings.
         */
        type BatchMultipleSettings = {
            /**
             * The list of batch requests. All URIs have to be relative URIs in the request.
             */
            readonly batchRequests: ReadonlyArray<BatchRequest>;
            /**
             * The endpoint to make the request to.
             * If not specified, will use the ARM endpoint.
             */
            readonly endpoint?: string;
            /**
             * Determines whether the ajax request is part of a background task.
             * If true the batch request will be pushed on to the background queue.
             */
            readonly isBackgroundTask?: boolean;
            /**
             * Determines whether to append a telemetry header for the ARM calls.
             *
             * Set to a non-empty string to append the header. The value should be 60 characters or less and will be trimmed
             * if longer.
             */
            readonly telemetryHeader?: string;
        };
        /**
         * Response for a request within a batch.
         */
        type BatchResponseItem<T> = {
            /**
             * The response content. Can be success or failure.
             */
            readonly content: T;
            /**
             * The response headers.
             */
            readonly headers: {
                [key: string]: string;
            };
            /**
             * The response status code.
             */
            readonly httpStatusCode: number;

            /**
             * The name provided in the request.
             */
            readonly name?: string;
        };
        /**
         * Batch response.
         */
        type BatchResponse = {
            /**
             * The success response from ARM.
             */
            readonly responses: ReadonlyArray<BatchResponseItem<any>>;
        };
        /**
         * Individual batch request.
         */
        type BatchRequest = {
            /**
             * The URI to call.
             */
            readonly uri: string;
            /**
             * The http method for the call. Defaults to GET
             */
            readonly httpMethod?: BatchHttpMethod;
            /**
             * Optional request details.
             */
            readonly requestHeaderDetails?: {
                /**
                 * The command name.
                 */
                readonly commandName?: string;
            };
            /**
             * The content to set on the request.
             */
            readonly content?: any;
        };
    }

    export namespace Marketplace {
        /**
         * Marketplace offer plan.
         */
        export interface OfferPlan {
            /**
             * The plan id.
             */
            planId: string;

            /**
             * The plan display name.
             */
            displayName: string;

            /**
             * The summary text for the plan.
             */
            summary: string;

            /**
             * The description HTML for the plan.
             */
            description: string;
        }

        /**
         * Marketplace offer pricing details model.
         * Used to retrieve the pricing information for a Marketplace offer.
         */
        export interface OfferPricingDetails {
            /**
             * The offer id.
             */
            offerId: string;

            /**
             * The publisher id.
             */
            publisherId: string;

            /**
             * The offer plans provided by the publisher.
             */
            plans: OfferPlan[];
        }

        /**
         * Marketplace product (offer).
         */
        export interface Product {
            /**
             * The product display name.
             */
            displayName: string;

            /**
             * The publisher display name.
             */
            publisherDisplayName: string;

            /**
             * The URI to the legal terms HTML.
             */
            legalTermsUri: string;

            /**
             * The URI to the privacy policy HTML.
             */
            privacyPolicyUri: string;

            /**
             * The other pricing details URI.
             */
            pricingDetailsUri: string;

            /**
             * The offer pricing details.
             */
            offerDetails?: OfferPricingDetails;
        }

        /**
         * Marketplace artifact.
         */
        export interface Artifact {
            /**
             * The artifact name.
             */
            name: string;

            /**
             * The URI to the artifact file.
             */
            uri: string;

            /**
             * The artifact type.
             */
            type: string; // tslint:disable-line:no-reserved-keywords
        }

        /**
         * The context from which a marketplace create is kicked off.
         */
        export interface LaunchingContext extends StringMap<any> {
            /**
             * The gallery item id.
             */
            galleryItemId: string;

            /**
             * The source entity launching the create flow (blade name, control, etc.). Used for telemetry logging.
             */
            source: string[];

            /**
             * The marketplace menu item id.
             */
            menuItemId?: string;

            /**
             * The marketplace sub menu item id.
             */
            subMenuItemId?: string;

            /**
             * The blade instance id.
             */
            bladeInstanceId?: string;
        }

        /**
         * Marketplace item.
         */
        export interface Item<TUIMetaData> {
            /**
             * The Marketplace item id.
             */
            id: string;

            /**
             * The item display name.
             */
            itemDisplayName: string;

            /**
             * The publisher display name.
             */
            publisherDisplayName: string;

            /**
             * The Marketplace item version.
             */
            version: string;

            /**
             * The list of category ids the Marketplace item belongs to.
             */
            categoryIds: string[];

            /**
             * The products associated with the Marketplace item.
             */
            products: Product[];

            /**
             * Marketplace item products with no pricing information.
             */
            productsWithNoPricing: Product[];

            /**
             * The artifacts associated with the Marketplace item.
             */
            specialArtifacts?: Artifact[];

            /**
             * The dictionary of metadata properties to be used by the extension.
             */
            metadata?: StringMap<string>;

            /**
             * The deployment name.
             */
            deploymentName: string;

            /**
             * The list of URIs for the CSM template files.
             */
            deploymentTemplateFileUris: StringMap<string>;

            /**
             * The list of URIs for the deployment fragments.
             */
            deploymentFragmentFileUris?: StringMap<string>;

            /**
             * The context from which a marketplace create is kicked off.
             */
            launchingContext: LaunchingContext;

            /**
             * Properties contained in the UIDefinition.json for the marketplace item
             */
            uiMetadata: TUIMetaData;
        }

        /**
         * The interface of context supplied by marketplace
         */
        export interface Context<TUIMetaData> {
            /**
             * The telemetry id; a GUID unique to each instance of the provisioning flow initiated by
             * the user (i.e. unique to each instance when the blade is launched). The same id is used
             * when the 'CreateFlowLaunched, 'ProvisioningStart/Ended' and 'CreateDeploymentStart/End'
             * events are logged. Adding this telemetry id to the telemetry logged on the blade will
             * help you connect all the data points for a given provisioning instance.
             */
            readonly telemetryId: string;

            /**
             * The Marketplace item invoking the blade. Will be undefined if 'requiresMarketplaceId'
             * is set to false on the @DoesProvisioning decorator options.
             */
            readonly marketplaceItem?: Item<TUIMetaData>;

            /**
             * The resource group name passed into the gallery when new is selected from a resource group
             */
            readonly resourceGroupName?: string;
        }
    }

    export namespace Provisioning {
        /**
         * The template output.
         */
        interface TemplateOutput {
            /**
             * The type of the output.
             */
            type: string;
            /**
             * The value of the output.
             */
            value: any;
        }
        /**
         * The template resource.
         */
        interface TemplateResource {
            /**
             * The name of the resource.
             */
            name: string;
            /**
             * The type of the resource.
             */
            type: string;
            /**
             * The API version of the resource.
             */
            apiVersion: string;
            /**
             * The location of the resource.
             */
            location: string;
            /**
             * The resource properties.
             */
            properties?: StringMap<any>;
            /**
             * The dependencies for this resource.
             */
            dependsOn?: string[];
            /**
             * The tags on the resource.
             */
            tags?: StringMap<string>;
            /**
             * Comments on the resource.
             */
            comments?: string;
            /**
             * The child resources.
             */
            resources?: TemplateResource[];
            /**
             * The resource id. Only includes in the validation response.
             */
            id?: string;
        }
        /**
         * The response that ARM returns when a template validate call succeeds.
         */
        interface TemplateValidationResponse {
            /**
             * Deployment id.
             */
            id: string;
            /**
             * Deployment name.
             */
            name: string;
            /**
             * Deployment properties.
             */
            properties: {
                /**
                 * Correlation id associated with the validate call.
                 */
                correlationId: string;
                /**
                 * Duration of validation.
                 */
                duration: string;
                /**
                 * Deployment mode.
                 */
                mode: string;
                /**
                 * Parameters passed to the validate call.
                 */
                parameters: StringMap<TemplateOutput>;
                /**
                 * Correlation id associated with the validate call.
                 */
                provisioningState: string;
                /**
                 * The timestamp.
                 */
                timestamp: string;
                /**
                 * The list of resources that are in the template.
                 */
                validatedResources: TemplateResource[];
            };
        }
        /**
         * The template deployment operation mode. Defaults to 'RequestDeploymentOnly'.
         */
        const enum TemplateDeploymentMode {
            /**
             * Submit a deployment request to ARM only (this does not wait till the resouces are provisioned).
             * The 'deployTemplate' API will return a promise that resolves with ARM's response to the request.
             */
            RequestDeploymentOnly = 1,
            /**
             * Submit a deployment request to ARM and wait till provisioning the resources has completed
             * (silent polling). The 'deployTemplate' API will return a promise that reports progress only
             * once, when the request is accepted. The promise resolves when provisioning the resources
             * has completed.
             */
            DeployAndAwaitCompletion = 2,
            /**
             * Submit a deployment request to ARM and wait till provisioning the resources has completed,
             * while reporting all updates from ARM. The 'deployTemplate' API will return a promise that
             * reports progress when the request is accepted, followed by all ARM operations on every poll.
             * The promise resolves when provisioning the resources has completed.
             */
            DeployAndGetAllOperations = 3,
            /**
             * Execute all the deployment preflight actions without submitting the deployment request
             * (sanity check, provisioning the resource group, registering the resource providers,
             * getting a valid deployment name, and running ARM's preflight validation).
             */
            PreflightOnly = 4,
        }
        /**
         * Initial values for form initialization. Use those values to initialize the subscription,
         * resource group, and location drop down controls.
         */
        export interface InitialValues {
            /**
             * The list of subscription ids last used by the user.
             */
            readonly subscriptionIds?: string[];
            /**
             * The list of location names last used by the user.
             */
            readonly locationNames?: string[];
            /**
             * The list of resource group names last used by the user.
             */
            readonly resourceGroupNames?: string[];
        }

        type Primitive = number | string | Date | boolean;

        interface StringMapPrimitive extends StringMap<StringMapRecursive | Primitive | Array<Primitive | StringMapRecursive>> { }

        type StringMapRecursive = StringMapPrimitive;

        /**
         * Options for validating the form prior to sending the preflight validation request to ARM.
         */
        export interface FormValidationOptions {
            /**
             * Explicitly prevent form validation.
             */
            readonly validateForm?: boolean;
            /**
             * Focus the first invalid control on the form. Defaults to false.
             */
            readonly focusOnFirstInvalid?: boolean;
            /**
             * Whether or not to validate hidden controls on the form. Defaults to true.
             */
            readonly validateHidden?: boolean;
        }

        /**
         * Options for the DeployTemplate method on provisioning context
         */
        export interface DeployTenantLevelTemplateOptions {
            /**
             * The deployment name.
             */
            deploymentName: string;
            /**
             * The resource id. Supply this to link the notifications to the asset or if the deployment
             * results in a startboard part.
             */
            resourceId?: string;
            /**
             * The context from which a gallery create is kicked off. Used for telemetry logging.
             */
            launchingContext?: Common.Marketplace.LaunchingContext;
            /**
             * Debug info.
             */
            debug?: string;
            /**
             * An array of the resource providers to be registered for the subscription.
             */
            resourceProviders: string[];
            /**
             * The parameters for the template deployment (name and value pairs).
             */
            parameters?: StringMapPrimitive;
            /**
             * The reference parameters for the template deployment.
             */
            referenceParameters?: StringMap<StringMapPrimitive>;
            /**
             * The URI for the parameters file. Use this to link to an existing parameters file. Specify
             * this or the parameters and/or referenceParameters properties, but not both.
             */
            parametersLinkUri?: string;
            /**
             * The URI for the ARM template. Specify this or the templateJson property, but not both.
             */
            templateLinkUri?: string;
            /**
             * The inline deployment template JSON. Specify this or the templateLinkUri property, but not both.
             */
            templateJson?: string;
            /**
             * The template deployment operation mode. Defaults to 'RequestDeploymentOnly'.
             */
            deploymentMode?: TemplateDeploymentMode;
            /**
             * Flag indicating that we should run ARM's preflight validation before submitting the template
             * deployment request to ARM. Defaults to false.
             */
            validateTemplate?: boolean;
            /**
             * The result of validating the template with ARM.
             */
            validationResult?: TemplateValidationResponse;
            /**
             * Options for validating the form before ARM validation.
             * This validation is enabled by default and can be disabled by setting validateForm = false in this property.
             */
            formValidationOptions?: FormValidationOptions;
            /**
             * The marketplaceId of the resource.
             */
            readonly marketplaceItemId?: string;
            /**
             * A key or hash that encodes or corresponds to information about the provisioning request.
             */
            readonly provisioningHash?: string;
            /**
             * Function to provide a part reference based on the resourceId of a deployment.
             * Defaults to the part reference provided by the marketplace UI.Definition file
             * or null if no marketplace item was provieded to this provisioning blade.
             *
             * @param resourceId The resourceId of the resource created
             */
            supplyPartReference?(resourceId: string): any; //PartReference<any>;
        }

        /**
         * Options for the DeployTemplate method at resource group level on provisioning context
         */
        export interface DeployTemplateOptions extends DeployTenantLevelTemplateOptions {
            /**
             * The subscription id.
             */
            subscriptionId: string;
            /**
             * The resource group name.
             */
            resourceGroupName: string;
            /**
             * The location/region.
             */
            resourceGroupLocation: string;
        }

        /**
         * Troubleshooting links for the arm errors blade
         */
        const enum TroubleshootingLinks {
            /**
             * Common Azure deployment errors
             */
            CommonDeploymentErrors = 0,
            /**
             * Move resources documentation
             */
            ResourceMoveDocs = 1,
            /**
             * Create ARM template documents
             */
            CreateArmTemplateDocs = 2,
        }

        /**
         * The input parameters for the arm errors blade.
         */
        interface ArmErrorsBladeParameters {
            /**
             * The errors object from ARM.
             */
            readonly errors: any;
            /**
             * The subscriptionId for the resource with an ARM error.
             * This is used to create a link to the quotas for the subscription.
             */
            readonly subscriptionId?: string;
            /**
             * The array of links to display in the "Troubleshooting links" section.
             */
            readonly troubleshootingLinks?: ReadonlyArray<TroubleshootingLinks>;
        }

        /**
         * Options for the DeployTemplate method at subscription level on provisioning context
         */
        export interface DeploySubscriptionLevelTemplateOptions extends DeployTenantLevelTemplateOptions {
            /**
             * The subscription id.
             */
            subscriptionId: string;
            /**
             * The location/region if this is a subscription level resource.
             */
            location: string;
        }

        /**
         * Options for the DeployTemplate method at subscription level on provisioning context
         */
        export interface DeployManagementGroupLevelTemplateOptions extends DeployTenantLevelTemplateOptions {
            /**
             * The managementGroup id.
             */
            managementGroupId: string;

            /**
             * The location/region if this is a managementGroup level resource.
             */
            location: string;
        }

        export type AllDeployTemplateOptions = DeployTemplateOptions | DeploySubscriptionLevelTemplateOptions | DeployTenantLevelTemplateOptions;
        interface StringMap<T> {
            [key: string]: T;
        }

        /**
         * ARM template deployment operation.
         */
        export interface TemplateDeploymentOperationProperties {
            /**
             * The resource being operated upon.
             */
            targetResource: StringMap<string>;
            /**
             * The timestamp when the operation was completed.
             */
            timestamp: string;
            /**
             * The unique id for this deployment operation.
             */
            trackingId: string;
            /**
             * The status of the operation.
             */
            statusCode: string;
            /**
             * The detailed status message for the operation returned by the resource provider.
             */
            statusMessage: string;
        }
        /**
         * ARM template deployment operation.
         */
        export interface TemplateDeploymentOperation {
            /**
             * The URI for the deployed entity.
             */
            id: string;
            /**
             * The operation id.
             */
            operationId: string;
            /**
             * The operation properties.
             */
            properties: TemplateDeploymentOperationProperties;
        }
        export const enum DeploymentStatusCode {
            /**
             * Template preflight, validation or deployment failure (based on the operation performed).
             */
            Failure = -1,
            /**
             * Deployment was accepted or successful (based on the operation performed).
             */
            Success = 0,
            /**
             * ARM rejected the deployment request.
             */
            DeploymentRequestFailed = 1,
            /**
             * Deployment failed.
             */
            DeploymentFailed = 2,
            /**
             * Deployment status unknown.
             */
            DeploymentStatusUnknown = 3,
            /**
             * An unexpected error occurred while provisioning the resource group.
             */
            ErrorProvisioningResourceGroup = 4,
            /**
             * An unexpected error occurred while submitting the deployment request.
             */
            ErrorSubmittingDeploymentRequest = 5,
            /**
             * An unexpected error occurred while getting the deployment status.
             */
            ErrorGettingDeploymentStatus = 6,
            /**
             * Invalid arguments.
             */
            InvalidArgs = 7,
            /**
             * An unexpected error occurred while registering the resource providers.
             */
            ErrorRegisteringResourceProviders = 8,
            /**
             * Deployment canceled.
             */
            DeploymentCanceled = 9,
            /**
             * Unknown error.
             */
            UnknownError = 10,
        }
        export interface BaseDeployTemplateResults {
            /**
             * The deployment status code.
             */
            deploymentStatusCode: DeploymentStatusCode;
            /**
             * The correlation id (aka tracking id).
             */
            correlationId: string;
            /**
             * The provisioning state.
             */
            provisioningState: string;
            /**
             * The timestamp when the operation was completed.
             */
            timestamp: Date;
            /**
             * The list of deployment operations.
             */
            operations?: TemplateDeploymentOperation[];
            /**
             * Timestamp when the deployment request was initiated.
             */
            requestTimestamp?: Date;
        }
        export type DeployTemplateResults<TOptions extends AllDeployTemplateOptions> = BaseDeployTemplateResults & TOptions;

        interface SimpleBladeReference {
            /**
             * The name of the extension that contains the Blade.
             */
            readonly extensionName: string;

            /**
             * The name of the Blade.
             */
            readonly bladeName: string;

            /**
             * A map of parameters to be passed to the Blade.
             */
            readonly parameters?: StringMap<any>;
        }

        /**
         * Options for the DeployCustom method on provisioning context
         */
        export interface DeployCustomOptions<TResult> {
            /**
             * A promise for when provisioning has finished
             */
            provisioningPromise: Promise<TResult>;
        }

        export interface Provisioning<TUIMetadata> extends Common.Marketplace.Context<TUIMetadata> {
            /**
             * Initial values for form initialization. Use those values to initialize the subscription,
             * resource group, and location drop down controls.
             */
            readonly initialValues: InitialValues;
            /**
             * Validates an ARM template and returns a promise for the validation result.
             * @param options Template deployment options
             */
            validateTemplate<TOptions extends AllDeployTemplateOptions = DeployTemplateOptions>(options: TOptions): Promise<TOptions>;
            /**
             * Deploy a template to ARM and receive a promise for a deployment result
             * @param options Template deployment options
             */
            deployTemplate<TOptions extends AllDeployTemplateOptions = DeployTemplateOptions>(options: TOptions): Promise<DeployTemplateResults<TOptions>>;
            /**
             * Get a blade reference to the template viewer blade
             * @param options Template deployment options
             */
            getAutomationBladeReference<TOptions extends AllDeployTemplateOptions = DeployTemplateOptions>(options: TOptions): Promise<SimpleBladeReference>;
            /**
             * Get a blade reference to the Arm Errors blade
             * @param bladeParameters Parameters passed to the arm errors blade
             */
            getArmErrorsBladeReference(bladeParameters: ArmErrorsBladeParameters): Promise<SimpleBladeReference>;
            /**
             * Deploy a template to ARM and recieve a promise for a deployment result
             * @param options Custom deployment options
             */
            deployCustom<TResult>(options: DeployCustomOptions<TResult>): Promise<TResult>;
        }
    }

    /**
     * Internal implementation use.
     */
    export namespace ProxiedFx {
        interface Inputs {
            ___ajax___getEndpoints: void;
            ___ajax___batch: Ajax.BatchSettings;
            ___ajax___batchMultiple: Ajax.BatchMultipleSettings;
            ___ajax___ajax: any;
            ___resources___getContentUri: string;
            ___resources___getAbsoluteUri: string;
        }
        interface Outputs {
            ___ajax___getEndpoints: Ajax.Endpoints;
            ___ajax___batch: Ajax.BatchResponseItem<any>;
            ___ajax___batchMultiple: Ajax.BatchResponse;
            ___ajax___ajax: any;
            ___resources___getContentUri: string;
            ___resources___getAbsoluteUri: string;
        }
    }
}

interface FxEnvironment {
    applicationPath: string;
    extensionName: string;
    sdkVersion: string;
    version: string;
}

declare namespace ReactView {
    function setFunctionProxyPort(port: MessagePort): void;
    export const enum Signature {
        ViewSuffix = ".ReactView",
        ModelSuffix = ".ReactModel",
        ReloadFeature = "reactreload",
    }
}
