namespace OmniDown.Models.Settings;

internal sealed record CloseBehaviorSettings(bool? MinimizeToTrayOnClose)
{
    public static CloseBehaviorSettings Default { get; } = new(MinimizeToTrayOnClose: null);
}
