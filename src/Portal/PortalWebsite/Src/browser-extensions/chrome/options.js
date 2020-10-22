'use strict';

(() => {
    const reloadTab = () => {
        chrome.tabs.query({ active: true, currentWindow: true }, (arrayOfTabs) => {
          const code = 'window.localStorage.clear(); window.location.reload();';
          const { url, id } = arrayOfTabs[0];

          const parseUrl = new URL(url);

          if (!parseUrl.hostname.endsWith('github.localhost')) {
              return;
          }

          chrome.tabs.executeScript(id, { code });
      });
    }

    const statusEl = document.getElementById('js-status');
    const enableExtensionEl = document.getElementById('js-enable-extension');
    const infoEls = document.getElementsByClassName('js-codespace-info');

    const getByPath = (obj, path) => path.split('.').reduce((result, key) => result ? result[key] : result, obj);
    const setByPath = (obj, path, value) => {
        let keys = path.split('.');
        const lastKey = keys.pop();
        for (let i = 0; i < keys.length; i++) {
            obj = obj[keys[i]] || (obj[keys[i]] = { });
        }
        if (value === '') {
            delete obj[lastKey];
        } else if (value === 'true') {
            value = true;
        } else if (value === 'false') {
            value = false;
        } else {
            obj[lastKey] = value;
        }
    };

    // Saves options to chrome.storage
    const saveOptions = () => {
        const isEnabled = enableExtensionEl.checked;

        let codespaceInfo = { }
        for (let i = 0; i < infoEls.length; i++) {
            setByPath(codespaceInfo, infoEls[i].name, infoEls[i].value);
        }

        const channel = codespaceInfo.vscodeSettings && codespaceInfo.vscodeSettings.vscodeChannel;

        document.body.classList.toggle('is-enabled', isEnabled);

        chrome.storage.sync.set({
            isEnabled,
            codespaceInfo,
        }, function() {
            statusEl.textContent = 'Options saved.';

            setTimeout(function() {
                statusEl.textContent = '';
            }, 1000);

            chrome.runtime.sendMessage({
                action: 'updateIcon',
                channel,
                isEnabled,
            });

            reloadTab();
        });
    }

    // Restores select box and checkbox state using the preferences
    // stored in chrome.storage.
    function restoreOptions() {
        chrome.storage.sync.get({
            isEnabled: true,
            isError: false,
            codespaceInfo: {},
        }, function(items) {
            enableExtensionEl.checked = typeof items.isEnabled === 'boolean'
                ? items.isEnabled
                : true;

            const channel = items.codespaceInfo.vscodeSettings && items.codespaceInfo.vscodeSettings.vscodeChannel;
            const isEnabled = enableExtensionEl.checked;
            const isError = items.isError;

            for (let i = 0; i < infoEls.length; i++) {
                infoEls[i].value = getByPath(items.codespaceInfo, infoEls[i].name) || '';
            } 

            document.body.classList.toggle('is-enabled', isEnabled);

            chrome.runtime.sendMessage({
                action: 'updateIcon',
                channel,
                isEnabled,
                isError,
            });
        });
    }

    document.addEventListener('DOMContentLoaded', restoreOptions);
    document.getElementById('js-save').addEventListener('click', saveOptions);
})();