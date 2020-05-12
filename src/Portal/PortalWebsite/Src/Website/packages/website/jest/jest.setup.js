window.crypto = window.crypto || {
    getRandomValues: () => {
        return new Buffer([Math.random(), Math.random()]);
    },
};

if (typeof window !== 'undefined') {
    // fetch() polyfill for making API calls.
    require('whatwg-fetch');
}
