window.houseLedgerDisplaySettings = {
    applyTextSize: function (textSize) {
        const sizeMap = {
            Small: "14px",
            Normal: "16px",
            Large: "18px"
        };

        const size = sizeMap[textSize] ?? sizeMap.Normal;
        document.documentElement.style.fontSize = size;
    }
};
