// RbacValues.ts

import { IEnvironmentNames, IPlaneNames } from "./ResourceNameDefs";

const supportedComponents = [
    'codesp',
]

export interface IRbacValues {
    getSubscriptionRbacArmTemplate: () => ArmTemplate;
}

export class ArmTemplate {
    public readonly '$schema': string = "https://schema.management.azure.com/schemas/2018-05-01/subscriptionDeploymentTemplate.json#";
    public readonly contentVersion: string = "1.0.0.0";
    public readonly parameters: IArmParameters;
    public readonly variables: IArmVariables;
    public readonly resources: IRoleAssignmentResource[];
    public readonly outputs: IArmOutputs;

    constructor(
        properties: IArmParameters,
        variables: IArmVariables,
        resources: IRoleAssignmentResource[],
        outputs: IArmOutputs) {
        this.parameters = properties ?? {};
        this.variables = variables ?? {};
        this.resources = resources ?? [];
        this.outputs = outputs ?? {};
    }
}

export interface IArmParameters {
    [index: string]: IArmParameterDefinition;
}

export interface IArmVariables {
    [index: string]: any;
}

export interface IArmParameterDefinition {
    type: string;
    defaultValue?: any;
}

export interface IArmOutputs {
    [index: string]: IArmOutputValue
}

export interface IArmOutputValue {
    type: string;
    value: any;
}

export interface IRoleResourceProperties {
    readonly description: string;
    readonly roleDefinitionId: string;
    readonly principalId: string;
    readonly principalType: string;
    readonly scope: string;
}

export interface IRoleAssignmentResource {
    readonly name: string;
    readonly type: string;
    readonly apiVersion: string;
    readonly condition: string;
    readonly properties: IRoleResourceProperties;
}

class ArmParameters implements IArmParameters {
    [index: string]: IArmParameterDefinition
}

class ArmParameterDefinition implements IArmParameterDefinition {
    public readonly type: string = "string";
    public readonly defaultValue?: string;

    constructor(defaultValue?: string) {
        this.defaultValue = defaultValue;
    }
}

class ArmVariables implements IArmVariables {
    [index: string]: any;
}

class ArmOutputs implements IArmOutputs {
    [index: string]: IArmOutputValue;
}

class ArmOutputValue implements IArmOutputValue {
    type: string;
    value: any;

    constructor(value: any, type = 'string') {
        this.type = type;
        this.value = value;
    }

}

enum PrincipalType {
    ServicePrincipal = "ServicePrincipal",
    Group = "Group"
}

interface IPrincipalIds {
    [index: string]: string;
    default?: string;
    tenant?: string;
    // For per-component service principals:
    //  core_dev:
    //  core_ppe:
    //  core_prod:
    // For per-environment service principals (like first party appid)
    //  dev:
    //  ppe:
    //  prod:
    // Otherwise:
    //  default:
}

enum TenantId {
    MSIT = "72f988bf-86f1-41af-91ab-2d7cd011db47",
    PME = "00000000-0000-0000-0000-000000000000",
    AME = "33e01921-4d64-4f8c-a055-5bdaffd5e33d",
}

class Principal {
    public readonly variableName: string;
    public readonly principalType: PrincipalType;
    public readonly ids: IPrincipalIds;
    public readonly required: boolean;

    constructor(id: string, principalType: PrincipalType, required: boolean, ids: IPrincipalIds) {
        this.variableName = id;
        this.principalType = principalType;
        this.ids = ids;
        this.required = required;
    }

    public getTenantId(names: IEnvironmentNames): string {
        return this.ids.tenant ?? names.environmentTenantId;
    }

    public get parameterReference(): string {
        return `parameters('${this.variableName}')`;
    }

    public get variableReference(): string {
        return `variables('${this.variableName}')`;
    }

    public get variableDefinition(): string {
        return `[${this.parameterReference}]`;
    }

    public get inTenantName(): string {
        return `${this.variableName}InTenant`
    }

    public get inTenantReference(): string {
        return `variables('${this.inTenantName}')`;
    }

    public getInTenantDefinition(names: IEnvironmentNames): string {
        const tenantId = this.getTenantId(names);
        return `[equals(subscription().tenantId,'${tenantId}')]`;
    }

    public getId(names: IPlaneNames): string {
        const idNames = [
            `${names.component}_${names.env}`,
            `${names.env}`,
        ];

        for (const idName of idNames) {
            const id = this.ids[idName];
            if (id) {
                return id;
            }
        }

        return this.ids.default;
    }
}

class SubscriptionRoleDefinition {
    public readonly roleName: string;
    public readonly roleId: string;

    constructor(roleName: string, roleId: string) {
        this.roleName = roleName;
        this.roleId = roleId;
    }

    public get resourceIdExpression(): string {
        return `subscriptionResourceId('Microsoft.Authorization/roleDefinitions','${this.roleId}')`;
    }
}

class RoleResourceProperties
    implements IRoleResourceProperties {
    public readonly roleDefinitionId: string;
    public readonly principalId: string;
    public readonly principalType: string;
    public readonly scope: string;
    public readonly description: string;

    public constructor(roleDefinition: SubscriptionRoleDefinition, principal: Principal) {
        this.description = `${principal.variableName} : ${roleDefinition.roleName}`;
        this.roleDefinitionId = `[${roleDefinition.resourceIdExpression}]`;
        this.principalId = `[${principal.variableReference}]`;
        this.principalType = principal.principalType;
        this.scope = "[concat('/subscriptions/',subscription().subscriptionId)]";
    }
}

class RoleAssignmentResource
    implements IRoleAssignmentResource {
    public readonly name: string;
    public readonly type: string = 'Microsoft.Authorization/roleAssignments';
    public readonly apiVersion: string = '2020-03-01-preview';
    public readonly condition: string;
    public readonly properties: RoleResourceProperties;

    public constructor(roleDefinition: SubscriptionRoleDefinition, principal: Principal) {
        // The name must be a unique but deterministic guid.
        // It is constructed using the guid() function with the role id as the base guid.
        // The assignee id and the subscription id (scope) are added for uniqueness.
        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-string#guid
        const assigneeExpression = `if(empty(${principal.variableReference}),'${principal.variableName}',${principal.variableReference})`;
        this.name = `[guid('${roleDefinition.roleId}',${assigneeExpression},subscription().subscriptionId)]`;

        // Skip this role assignment if the principal ID has not been specified as a parameter.
        // Skip this role assignment if the prindipal doesn't exist in the subscription tenatn.
        this.condition = `[and(not(empty(${principal.variableReference})),${principal.inTenantReference})]`;

        // Role assignment properties
        this.properties = new RoleResourceProperties(roleDefinition, principal);
    }
}

// Principal Assignee Variables
const breakGlassGroup = new Principal("breakGlassGroupId", PrincipalType.Group, true, {
    default: "86701306-e3cd-4b17-85a1-2956e25a2527"
});
const opsSp = new Principal("opsSpId", PrincipalType.ServicePrincipal, true, {
    core_dev: "b0f95b67-80dd-4cc5-9a13-7cc2adbb64ec",
    core_ppe: undefined, // TODO
    core_prod: undefined, // TODO
    codesp_dev: "5b2aa733-8f92-4064-98c3-a5f71d2b4fe6",
    codesp_ppe: undefined, // TODO
    codesp_prod: undefined, // TODO
});
const opsMi = new Principal("opsMiId", PrincipalType.ServicePrincipal, false, {
    core_dev: "65b35caf-5cf1-4d88-9a8f-c4f96990b62d",
});
const appSp = new Principal("appSpId", PrincipalType.ServicePrincipal, true, {
    core_dev: undefined, // TODO
    codesp_dev: "e1556ad6-4378-4f10-9159-006524611b63",
    codesp_ppe: undefined, // TODO
    codesp_prod: undefined, // TODO
});
const appMi = new Principal("appMiId", PrincipalType.ServicePrincipal, true, {
    core_dev: undefined, // TODO
    codesp_dev: undefined, // TODO
    codesp_ppe: undefined, // TODO
    codesp_prod: undefined, // TODO
});
const firstPartyApp = new Principal("firstPartyAppSpId", PrincipalType.ServicePrincipal, true, {
    dev: "a0b50ae5-6a18-4e3d-b553-f5c7d4e5b87e",
    default: "944cc140-b92f-4bce-a09a-426e827a040c",
});
const teamAdminsGroup = new Principal("teamAdminsGroupId", PrincipalType.Group, true, {
    default: "0433b29f-19b4-44cb-92a5-d30951ad2bf1",
});
const teamContributorsGroup = new Principal("teamContributorsGroupId", PrincipalType.Group, true, {
    default: "76ed1206-72df-4116-8e6a-747439d31855",
});
const teamReadersGroup = new Principal("teamReadersGroupId", PrincipalType.Group, true, {
    default: "6837c2b1-4f15-45e3-a9f5-9bfac0726a47",
});
const jitAdminsGroup = new Principal("jitAdminsGroupId", PrincipalType.Group, false, {
    tenant: TenantId.AME,
    ppe: '',
    prod: '',
});
const jitDataAdminsGroup = new Principal("jitDataAdminsGroupId", PrincipalType.Group, false, {
    tenant: TenantId.AME,
    ppe: '',
    prod: '',
});
const jitDataSecretsAdminsGroup = new Principal("jitDataSercretsAdminsGroupId", PrincipalType.Group, false, {
    tenant: TenantId.AME,
    ppe: '',
    prod: '',
});
const jitContributorsGroup = new Principal("jitContributorsGroupId", PrincipalType.Group, false, {
    tenant: TenantId.AME,
    ppe: '',
    prod: '',
});
const jitDataContributorsGroup = new Principal("jitDataContributorsGroupId", PrincipalType.Group, false, {
    tenant: TenantId.AME,
    ppe: '',
    prod: '',
});
const jitDataSecretsContributorsGroup = new Principal("jitDataSecretsContributorsGroupId", PrincipalType.Group, false, {
    tenant: TenantId.AME,
    ppe: '',
    prod: '',
});
const jitReadersGroup = new Principal("jitReadersGroup", PrincipalType.Group, false, {
    tenant: TenantId.AME,
    ppe: '',
    prod: '',
});
const jitDataReadersGroup = new Principal("jitDataReadersGroup", PrincipalType.Group, false, {
    tenant: TenantId.AME,
    ppe: '',
    prod: '',
});
const jitDataSecretsReadersGroup = new Principal("jitDataSecretsReadersGroup", PrincipalType.Group, false, {
    tenant: TenantId.AME,
    ppe: '',
    prod: '',
});

// Azure role defintitions
const acrPullRole = new SubscriptionRoleDefinition("AcrPull", "7f951dda-4ed3-4680-a7ca-43fe172d538d");
const acrPushRole = new SubscriptionRoleDefinition("AcrPush", "8311e382-0749-4cb8-b61a-304f252e45ec");
const contributorRole = new SubscriptionRoleDefinition("Contributor", "b24988ac-6180-42a0-ab88-20f7382dd24c");
const cosmosDbAccountReader = new SubscriptionRoleDefinition("CosmosDB Account Reader", "fbdf93bf-df7d-467e-a4d2-9458aa1360c8");
const keyVaultAdminRole = new SubscriptionRoleDefinition("Key Vault Administrator", "00482a5a-887f-4fb3-b363-3b7fe8e74483");
const keyVaultReaderRole = new SubscriptionRoleDefinition("Key Vault Reader", "21090545-7ca7-4776-b22c-e363652d74d2");
const keyVaultSecretsUserRole = new SubscriptionRoleDefinition("Key Vault Secrets User", "4633458b-17de-408a-b874-0445c86b69e6");
const ownerRole = new SubscriptionRoleDefinition("Owner", "8e3af657-a8ff-443c-a75c-2fe8c4bcb635");
const readerRole = new SubscriptionRoleDefinition("Reader", "acdd72a7-3385-48ef-bd42-f606fba81ae7");

// Owner roles
const ownerRoles = [
    ownerRole,
];

// Reader roles
const dataSecretsReaderRoles = [
    keyVaultReaderRole,
];
const dataReaderRoles = [
    readerRole,
];
const readerRoles = dataReaderRoles.concat(dataSecretsReaderRoles);

// Contributor roles
const dataSecretsContributorRoles = dataSecretsReaderRoles.concat([
    keyVaultSecretsUserRole,
]);
const dataContributorRoles = dataReaderRoles.concat([
    contributorRole,
    cosmosDbAccountReader,
    acrPullRole,
    acrPushRole,
]);
const contributorRoles = dataContributorRoles.concat(dataSecretsContributorRoles);

// Admin roles
const dataSecretsAdminRoles = dataSecretsContributorRoles.concat([
    keyVaultAdminRole,
]);
const dataAdminRoles = dataContributorRoles.concat([
]);
const adminRoles = dataAdminRoles.concat(dataSecretsAdminRoles);

// RbacValues
export class RbacValues {
    private readonly names: IPlaneNames;

    private constructor(plane: IPlaneNames) {
        this.names = plane;
    }

    public static assignValues(target: IPlaneNames): IRbacValues {
        const instance = new RbacValues(target);
        const rbacValues = instance.getRbacValues();
        Object.assign(target, rbacValues);
        return rbacValues;
    }

    public getRbacValues(): IRbacValues {

        let roleAssignmentResources: RoleAssignmentResource[] = [];
        const parameters = new ArmParameters();
        const variables = new ArmVariables();
        const outputs = new ArmOutputs();
        const supportedComponent = supportedComponents.includes(this.names.component);
        outputs['isSupportedComponent'] = new ArmOutputValue(supportedComponent, 'bool');
        outputs['component'] = new ArmOutputValue(this.names.component);
        outputs['env'] = new ArmOutputValue(this.names.env);
        outputs['plane'] = new ArmOutputValue(this.names.plane);
        const names = this.names;

        function addRoleAssignments(
            principal: Principal,
            roles: SubscriptionRoleDefinition[],
            condition = true) {

            if (condition) {
                const roleAssignments = roles.map(role => new RoleAssignmentResource(role, principal));

                if (roleAssignments !== undefined && roleAssignments.length != 0) {
                    roleAssignmentResources = roleAssignmentResources.concat(roleAssignments);

                    const name = principal.variableName;

                    if (!(name in parameters)) {
                        const id = principal.getId(names);
                        const defaultValue = id ? id : principal.required ? undefined : '';
                        parameters[name] = new ArmParameterDefinition(defaultValue);
                    }

                    if (!(name in variables)) {
                        variables[name] = principal.variableDefinition;
                        variables[principal.inTenantName] = principal.getInTenantDefinition(names);
                    }

                    if (!(name in outputs)) {
                        outputs[name] = new ArmOutputValue(`[${principal.variableReference}]`);
                    }
                }
            }
        }

        if (supportedComponent) {
            // Conditions for assignments
            const isDataPlane = this.isDataPlane();
            const isProd = this.isEnvironment('prod');
            const isDev = this.isEnvironment('dev');
            const isPpe = this.isEnvironment('ppe');
            const isJit = isPpe || isProd;
            const isJitNonData = isJit && !isDataPlane;
            const isJitDataOnly = isJit && isDataPlane;

            // Add role assignments
            // addRoleAssignments(opsMi, contributorRoles, !isDataPlane);
            addRoleAssignments(opsSp, contributorRoles, !isDataPlane);
            // addRoleAssignments(appMi, contributorRoles);
            addRoleAssignments(appSp, contributorRoles);
            addRoleAssignments(breakGlassGroup, ownerRoles);
            addRoleAssignments(teamAdminsGroup, ownerRoles, isDev);
            addRoleAssignments(teamContributorsGroup, contributorRoles, isDev);
            addRoleAssignments(teamReadersGroup, readerRoles, !isProd);
            addRoleAssignments(firstPartyApp, dataContributorRoles, isDataPlane);
            addRoleAssignments(jitAdminsGroup, adminRoles, isJitNonData);
            addRoleAssignments(jitContributorsGroup, contributorRoles, isJitNonData);
            addRoleAssignments(jitDataAdminsGroup, dataAdminRoles, isJitDataOnly);
            addRoleAssignments(jitDataContributorsGroup, dataContributorRoles, isJitDataOnly);
            addRoleAssignments(jitDataReadersGroup, dataReaderRoles, isJitDataOnly);
            addRoleAssignments(jitDataSecretsAdminsGroup, dataSecretsAdminRoles, isJitDataOnly);
            addRoleAssignments(jitDataSecretsContributorsGroup, dataSecretsContributorRoles, isJitDataOnly);
            addRoleAssignments(jitDataSecretsReadersGroup, dataSecretsReaderRoles, isJitDataOnly);
            addRoleAssignments(jitReadersGroup, readerRoles, isJitNonData);
        }

        // Emit the ARM template in deterministic (sorted) ordering.
        const armTemplate = new ArmTemplate(
            RbacValues.sortKeys(parameters),
            RbacValues.sortKeys(variables),
            roleAssignmentResources.sort((a,b) => a.properties.description.localeCompare(b.properties.description)),
            RbacValues.sortKeys(outputs));

        const rbacValues: IRbacValues = {
            getSubscriptionRbacArmTemplate: () => armTemplate,
        };

        return rbacValues;
    }

    private isDataPlane(): boolean {
        return this.names.plane === 'data';
    }

    private isEnvironment(env: string) {
        return this.names.env === env;
    }

    // TODO: move to Helpers, duplicate of ResourceNames.sortKeys
    private static sortKeys<T>(obj_1: T): T {
        const key = Object.keys(obj_1)
            .sort(function order(key1, key2) {
                if (key1 < key2) return -1;
                else if (key1 > key2) return +1;
                else return 0;
            });

        // Taking the object in 'temp' object
        // and deleting the original object.
        const temp = {};

        for (let i = 0; i < key.length; i++) {
            temp[key[i]] = obj_1[key[i]];
            delete obj_1[key[i]];
        }

        // Copying the object from 'temp' to
        // 'original object'.
        for (let i = 0; i < key.length; i++) {
            obj_1[key[i]] = temp[key[i]];
        }
        return obj_1;
    }
}