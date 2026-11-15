namespace HouseLedger.Client.UserService;

public sealed class DisplaySettingsState
{
    public bool DarkModeEnabled { get; private set; }
    public string TextSize { get; private set; } = "Normal";

    public event Action? Changed;

    public void Update(bool darkModeEnabled, string? textSize)
    {
        var normalizedTextSize = textSize is "Small" or "Normal" or "Large" ? textSize : "Normal";
        if (DarkModeEnabled == darkModeEnabled && string.Equals(TextSize, normalizedTextSize, StringComparison.Ordinal))
        {
            return;
        }

        DarkModeEnabled = darkModeEnabled;
        TextSize = normalizedTextSize;
        Changed?.Invoke();
    }
}
