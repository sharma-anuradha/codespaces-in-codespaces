const fs = require('fs');

const paths = require('./paths');

const extendedSchema = require(paths.extendedSchemaPath);

const rootSettingsToRemove = [
    'favicon'
];

const vscodeSettingsToRemove = [
    'enableSyncByDefault',
    'authenticationSessionId',
    'homeIndicator'
];

// delete vscode properties
for (let propertyName of vscodeSettingsToRemove) {
    delete extendedSchema.properties.vscodeSettings.properties[propertyName];
}

// delete root properties
for (let propertyName of rootSettingsToRemove) {
    delete extendedSchema.properties[propertyName];
}

const { required } = extendedSchema.properties.vscodeSettings;
extendedSchema.properties.vscodeSettings.required = required.filter((propertyName) => {
    return !vscodeSettingsToRemove.includes(propertyName);
});

// update $id
extendedSchema['$id'] = extendedSchema['$id'].replace(/\-extended$/, '');

fs.writeFileSync(paths.baseSchemaPath, JSON.stringify(extendedSchema, null, 4));
