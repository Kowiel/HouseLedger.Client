window.houseLedgerAuthStorage = (() => {
    const tabIdKey = "houseLedger.tabId";
    const subscriptions = new Map();
    let nextSubscriptionId = 0;

    return {
        getTabId() {
            let tabId = sessionStorage.getItem(tabIdKey);
            const navigationType = performance.getEntriesByType("navigation")[0]?.type;
            const openedFromAnotherTab = Boolean(window.opener) && navigationType !== "reload";
            if (!tabId || openedFromAnotherTab) {
                tabId = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`;
                sessionStorage.setItem(tabIdKey, tabId);
            }

            return tabId;
        },
        get(key) {
            return localStorage.getItem(key);
        },
        set(key, value) {
            localStorage.setItem(key, value);
        },
        remove(key) {
            localStorage.removeItem(key);
        },
        subscribe(key, dotNetReference) {
            const subscriptionId = ++nextSubscriptionId;
            const listener = event => {
                if (event.key === key) {
                    dotNetReference.invokeMethodAsync("OnAuthStorageChanged", event.newValue).catch(() => {});
                }
            };

            window.addEventListener("storage", listener);
            subscriptions.set(subscriptionId, listener);
            return subscriptionId;
        },
        unsubscribe(subscriptionId) {
            const listener = subscriptions.get(subscriptionId);
            if (listener) {
                window.removeEventListener("storage", listener);
                subscriptions.delete(subscriptionId);
            }
        }
    };
})();
