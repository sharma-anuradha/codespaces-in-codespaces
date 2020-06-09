const path = require('path');
const Validator = require('jsonschema').Validator;

const paths = require('./paths');

const schemaSamplesRootPath = path.join(paths.schemasRootPath, './samples/');

const extendedSchemaSamplePath = path.join(schemaSamplesRootPath, './schema-extended-sample.json');
const baseSchemaSamplePath = path.join(schemaSamplesRootPath, './schema-base-sample.json');

const v = new Validator();

const validateSchema = (samplePath, schemaPath, successMessage) => {
    const validationResult = v.validate(require(samplePath), require(schemaPath));
    if (validationResult.throwError || validationResult.errors.length) {
        throw new Error(validationResult.throwError || validationResult.errors[0]);
    } else {
        console.log(successMessage);
    }

};

console.log(`\n`);

// Validate extended schema
validateSchema(
    extendedSchemaSamplePath,
    paths.extendedSchemaPath,
    `✔ Extended JSON schema is valid.`,
);

// Validate base schema
validateSchema(
    baseSchemaSamplePath,
    paths.baseSchemaPath,
    `✔ Base JSON schema is valid.`,
);

// Extended schema should also accept the base schema sample
validateSchema(
    baseSchemaSamplePath,
    paths.extendedSchemaPath,
    `✔ Extended schema validates base sample.`,
);

// Base schema should fail on extended schema sample
const baseSchemaInvalidValidationResult = v.validate(
    require(extendedSchemaSamplePath),
    require(paths.baseSchemaPath)
);
if (!baseSchemaInvalidValidationResult.throwError && !baseSchemaInvalidValidationResult.errors.length) {
    throw new Error('The base schema is too permisive and works for the extended sample.');
} else {
    console.log(`✔ Base schema is not valid for the extended sample.`);
}
