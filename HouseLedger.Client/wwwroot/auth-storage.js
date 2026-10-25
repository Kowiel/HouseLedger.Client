window.houseLedgerAuthStorage = (() => {
    const tabIdKey = "houseLedger.tabId";
    const subscriptions = new Map();
    let nextSubscriptionId = 0;
    let inactivityTimer;
    let inactivityCallback;
    let inactivityTimeoutMilliseconds;
    const activityEvents = ["pointerdown", "pointermove", "keydown", "scroll", "touchstart"];

    const stopInactivityMonitor = () => {
        clearTimeout(inactivityTimer);
        inactivityTimer = undefined;
        activityEvents.forEach(eventName => window.removeEventListener(eventName, resetInactivityTimer));
        inactivityCallback = undefined;
    };

    const resetInactivityTimer = () => {
        clearTimeout(inactivityTimer);
        inactivityTimer = window.setTimeout(() => {
            const callback = inactivityCallback;
            stopInactivityMonitor();
            callback?.invokeMethodAsync("OnInactivityTimeoutAsync").catch(() => {});
        }, inactivityTimeoutMilliseconds);
    };

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
        },
        startInactivityMonitor(timeoutMilliseconds, dotNetReference) {
            stopInactivityMonitor();
            inactivityTimeoutMilliseconds = Math.max(0, timeoutMilliseconds);
            inactivityCallback = dotNetReference;
            activityEvents.forEach(eventName => window.addEventListener(eventName, resetInactivityTimer, { passive: true }));
            resetInactivityTimer();
        },
        stopInactivityMonitor() {
            stopInactivityMonitor();
        }
    };
})();
