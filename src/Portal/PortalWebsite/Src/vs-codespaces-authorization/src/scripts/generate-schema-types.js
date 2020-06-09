const fs = require('fs');
const path = require('path');
const paths = require('./paths');

const { compileFromFile } = require('json-schema-to-typescript');

const typesRootPath = path.join(__dirname, '../types/');
const baseSchemaTypesPath = path.join(typesRootPath, 'schema-base.d.ts');
const extendedSchemaTypesPath = path.join(typesRootPath, 'schema-extended.d.ts');

if (!fs.existsSync(typesRootPath)){
    fs.mkdirSync(typesRootPath);
}

compileFromFile(paths.baseSchemaPath)
    .then(ts => fs.writeFileSync(baseSchemaTypesPath, ts));

compileFromFile(paths.extendedSchemaPath)
    .then(ts => fs.writeFileSync(extendedSchemaTypesPath, ts));
