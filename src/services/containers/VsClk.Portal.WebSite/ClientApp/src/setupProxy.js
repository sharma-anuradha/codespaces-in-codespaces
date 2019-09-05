// @ts-check

const https = require('https');

// For development purposes, do not import
/**
 * @param {import("express").Application} app
 */
module.exports = (app) => {
    // Environment registration service
    app.use((_, response, next) => {
        response.setHeader('Service-Worker-Allowed', '/');
        next();
    });

    // VSCode source maps
    app.use((req, res, next) => {
        const workbenchBaseUrl = '/static/web-standalone/out/vs/workbench/';
        const sourcemapsHost = 'https://ticino.blob.core.windows.net/sourcemaps';

        if (req.originalUrl.startsWith(`${workbenchBaseUrl}${sourcemapsHost}`)) {
            https.get(req.originalUrl.substr(workbenchBaseUrl.length), (sourcemapResult) => {
                sourcemapResult.pipe(res);
            });
        } else {
            next();
        }
    });
};
