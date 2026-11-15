window.houseLedgerInactivity = (() => {
    let inactivityTimer;
    let warningTimer;
    let inactivityCallback;
    let inactivityTimeoutMilliseconds;
    let warningLeadTimeMilliseconds;
    let warningIsActive = false;
    const activityEvents = ["pointerdown", "pointermove", "keydown", "scroll", "touchstart"];

    const stopInactivityMonitor = () => {
        clearTimeout(inactivityTimer);
        clearTimeout(warningTimer);
        inactivityTimer = undefined;
        warningTimer = undefined;
        warningIsActive = false;
        activityEvents.forEach(eventName => window.removeEventListener(eventName, resetInactivityTimer));
        inactivityCallback = undefined;
    };

    const resetInactivityTimer = () => {
        if (warningIsActive) {
            return;
        }

        clearTimeout(inactivityTimer);
        clearTimeout(warningTimer);
        const warningDelay = Math.max(0, inactivityTimeoutMilliseconds - warningLeadTimeMilliseconds);
        warningTimer = window.setTimeout(() => {
            warningIsActive = true;
            inactivityCallback?.invokeMethodAsync("OnSessionExpiringAsync").catch(() => {});
            inactivityTimer = window.setTimeout(() => {
                const callback = inactivityCallback;
                stopInactivityMonitor();
                callback?.invokeMethodAsync("OnInactivityTimeoutAsync").catch(() => {});
            }, inactivityTimeoutMilliseconds - warningDelay);
        }, warningDelay);
    };

    return {
        startInactivityMonitor(timeoutMilliseconds, warningLeadMilliseconds, dotNetReference) {
            stopInactivityMonitor();
            inactivityTimeoutMilliseconds = Math.max(0, timeoutMilliseconds);
            warningLeadTimeMilliseconds = Math.min(inactivityTimeoutMilliseconds, Math.max(0, warningLeadMilliseconds));
            inactivityCallback = dotNetReference;
            activityEvents.forEach(eventName => window.addEventListener(eventName, resetInactivityTimer, { passive: true }));
            resetInactivityTimer();
        },
        confirmActivity() {
            if (!warningIsActive || !inactivityCallback) {
                return;
            }

            warningIsActive = false;
            resetInactivityTimer();
        },
        stopInactivityMonitor() {
            stopInactivityMonitor();
        }
    };
})();
