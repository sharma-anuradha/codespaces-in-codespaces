if (!/yarn\.js$|yarnpkg$/.test(process.env['npm_execpath'])) {
    console.error('\033[1;31m*** Please use yarn to install dependencies.\033[0;0m\n');
    process.exit(1);
}
