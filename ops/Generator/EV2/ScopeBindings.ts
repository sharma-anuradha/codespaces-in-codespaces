/** A document that defines the bindings for resource parameters, configuration files and any custom payloads files for extensions. It is required to use the Parameterization feature and otherwise optional. */
export interface IScopeBindings {
    /** The list of scope binding definitions. */
    scopeBindings: IScopeBinding[];
    
   /** The version of the schema that a document conforms to. */
   contentVersion: string;
}

export interface IScopeBinding {
    /** The unique tag name tied to a service resource group or a service resource. */
    scopeTagName: string;
    /** The list of bindings related to a service resource group or a service resource. */
    bindings: IBindings[];
}

export interface IBindings {
    /** The placeholder to be replaced. */
    find: string;
    /** The value to replace the placeholder. */
    replaceWith: string;
}