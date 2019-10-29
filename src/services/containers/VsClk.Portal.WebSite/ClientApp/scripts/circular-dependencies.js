const colors = require('colors/safe');
const madge = require('madge');

madge('./src', { fileExtensions: 'ts' }).then((result) => {
    var circular = result.circular();

    if (circular.length > 0) {
        const dependenciesString = circular
            .map(function(paths, i) {

                const redPaths = paths.map(function(path) {
                    return colors.magenta(path);
                });

                return colors.gray((i + 1) + ') ') + redPaths.join(colors.gray(' > '));
            })
            .join('\n');

        throw new Error(colors.red('\n\nCircular dependencies found!\n\n') + dependenciesString);
    }
}).catch((e) => {
    // catch the error for now, until we solve all circular deps
    console.log(e);
});