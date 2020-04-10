
window.crypto = window.crypto || {
    getRandomValues: () => {
        return new Buffer([Math.random(), Math.random()]);
    }
};
