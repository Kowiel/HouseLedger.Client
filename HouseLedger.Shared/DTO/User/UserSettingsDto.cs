using System.ComponentModel.DataAnnotations;

namespace HouseLedger.Shared.DTO.User;

public class UserSettingsDto
{
    public bool DarkModeEnabled { get; set; }

    [Required]
    [RegularExpression("^(Small|Normal|Large)$")]
    public string TextSize { get; set; } = "Normal";

    public bool EmailNotificationsEnabled { get; set; }
    public bool WeeklySummaryEnabled { get; set; }
}
