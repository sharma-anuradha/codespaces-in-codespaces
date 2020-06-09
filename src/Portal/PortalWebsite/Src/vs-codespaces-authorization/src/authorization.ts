import { Validator, Schema } from 'jsonschema';

import { VSCodespacesPlatformInfo, VSCodespacesPlatformInfoInternal } from './types';

const validator = new Validator();

const baseSchema = require('./schemas/schema-base.json');
const extendedSchema = require('./schemas/schema-extended.json');

const authorizePlatformUtility = async (
    endpoint: string,
    data: VSCodespacesPlatformInfo | VSCodespacesPlatformInfoInternal
) => {
    const { codespaceToken } = data;

    if (!codespaceToken) {
        throw new Error('No `Codespace` token set.');
    }

    // Create <form /> element to initiate the top-level HTTP POST request.
    const formEl = document.createElement('form');
    const originUrl = new URL(endpoint);
    formEl.setAttribute('action', originUrl.toString());
    formEl.setAttribute('method', 'POST');

    // create the hidden input elements
    const codespaceTokenInput = document.createElement('input');
    const partnerInfoInput = document.createElement('input');

    // set the Codespace token
    codespaceTokenInput.name = 'codespaceToken';
    codespaceTokenInput.value = codespaceToken;

    // set the partner info
    partnerInfoInput.name = 'partnerInfo';
    partnerInfoInput.value = JSON.stringify(data);

    // append the form to the DOM
    formEl.appendChild(codespaceTokenInput);
    formEl.appendChild(partnerInfoInput);
    document.body.append(formEl);

    // submit the form to initiate the top-level HTTP POST
    formEl.submit();
};

/**
 * Utility to validate a payload with a JSON schema.
 * @param instance Payload.
 * @param schema JSON schema.
 */
const validateBySchemaUtility = (
    instance: VSCodespacesPlatformInfo | VSCodespacesPlatformInfoInternal,
    schema: Schema
) => {
    const validationResult = validator.validate(instance, schema);
    if (validationResult.errors.length) {
        throw new Error(validationResult.errors[0].message);
    }
};

/**
 * Function to authorize VSCS platform workbench with a top-level HTTP POST.
 *
 * @param endpoint VSCS authentication endpoint.
 * @param data VSCS partner info (VSCodespacesPlatformInfo).
 */
export const authorizePlatform = async (endpoint: string, data: VSCodespacesPlatformInfo) => {
    validateBySchemaUtility(data, baseSchema);

    return await authorizePlatformUtility(endpoint, data);
};

/**
 * Function to authorize VSCS platform workbench with a top-level HTTP POST with internal API surface.
 *
 * @param endpoint VSCS authentication endpoint.
 * @param data VSCS partner info (VSCodespacesPlatformInfoInternal).
 */
export const authorizePlatformInternal = async (
    endpoint: string,
    data: VSCodespacesPlatformInfoInternal
) => {
    validateBySchemaUtility(data, extendedSchema);

    return await authorizePlatformUtility(endpoint, data);
};
