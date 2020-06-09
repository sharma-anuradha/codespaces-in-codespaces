const fs = require('fs');

const paths = require('./paths');

const extendedSchema = require(paths.extendedSchemaPath);

const vscodeSettingsToRemove = [
    'enableSyncByDefault',
    'authenticationSessionId',
    'homeIndicator',
];
// delete internal properties
for (let propertyName of vscodeSettingsToRemove) {
    delete extendedSchema.properties.vscodeSettings.properties[propertyName];
}
const { required } = extendedSchema.properties.vscodeSettings;
extendedSchema.properties.vscodeSettings.required = required.filter((propertyName) => {
    return !vscodeSettingsToRemove.includes(propertyName);
});

fs.writeFileSync(paths.baseSchemaPath, JSON.stringify(extendedSchema, null, 4));
