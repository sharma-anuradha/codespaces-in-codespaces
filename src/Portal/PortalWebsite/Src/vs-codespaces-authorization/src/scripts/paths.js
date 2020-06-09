const path = require('path');

const schemasRootPath = path.join(__dirname, '../schemas/');

module.exports = {
    schemasRootPath,
    extendedSchemaPath: path.join(schemasRootPath, './schema-extended.json'),
    baseSchemaPath: path.join(schemasRootPath, './schema-base.json'),
};
