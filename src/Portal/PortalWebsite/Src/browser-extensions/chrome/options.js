'use strict';

(() => {
    const reloadTab = () => {
        chrome.tabs.query({ active: true, currentWindow: true }, (arrayOfTabs) => {
          const code = 'window.location.reload();';
          const { url, id } = arrayOfTabs[0];

          const parseUrl = new URL(url);

          if (!parseUrl.pathname.startsWith('/codespace') || parseUrl.hostname !== 'github.com') {
              return;
          }

          chrome.tabs.executeScript(id, { code });
      });
    }

    const statusEl = document.getElementById('js-status');
    const vscodeChannelEl = document.getElementById('js-vscode-channel');
    const enableExtensionEl = document.getElementById('js-enable-extension');

    // Saves options to chrome.storage
    const saveOptions = () => {
        const channel = vscodeChannelEl.value;

        const isEnabled = enableExtensionEl.checked;

        document.body.classList.toggle('is-enabled', isEnabled);

        chrome.storage.sync.set({
            isEnabled: isEnabled,
            vscodeChannel: channel,
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
            vscodeChannel: true,
            isEnabled: true,
            isError: true
        }, function(items) {
            vscodeChannelEl.value = items.vscodeChannel;

            enableExtensionEl.checked = typeof items.isEnabled === 'boolean'
                ? items.isEnabled
                : true;

            const isEnabled = enableExtensionEl.checked;
            
            document.body.classList.toggle('is-enabled', isEnabled);

            chrome.runtime.sendMessage({
                action: 'updateIcon',
                channel: items.vscodeChannel,
                isEnabled,
                isError,
            });
        });
    }

    document.addEventListener('DOMContentLoaded', restoreOptions);
    document.getElementById('js-save').addEventListener('click', saveOptions);
})();