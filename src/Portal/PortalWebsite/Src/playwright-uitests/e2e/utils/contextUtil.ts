async function contextUtil(context:any) {
    const cookies = process.env.COOKIES;
    const localStorage = process.env.LOCALSTORAGE;
    
    // Some apps only need cookies (not local storage)
    await context.addCookies(JSON.parse(cookies));
    await context.addInitScript(([storageDump]) => {
      if (window.location.hostname === 'github.com') {
        console.log(window.location.hostname);
        console.log(storageDump.length);
        const entries = JSON.parse(storageDump);
        Object.keys(entries).forEach(k => {
          window.localStorage.setItem(k, entries[k]);
        });
      }
    }, [localStorage]);
}

module.exports = {contextUtil};