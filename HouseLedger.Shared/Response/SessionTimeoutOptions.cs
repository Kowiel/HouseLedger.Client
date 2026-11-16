using System.ComponentModel.DataAnnotations;

namespace HouseLedger.Shared.Response;

public sealed class SessionTimeoutOptions
{
    public const string SectionName = "Session";

    [Range(1, int.MaxValue)]
    public int AbsoluteLifetimeHours { get; set; } = 8;

    [Range(1, int.MaxValue)]
    public int InactivityTimeoutMinutes { get; set; } = 10;
}
