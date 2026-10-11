using System;

namespace YALCY.Integrations;

/// <summary>
/// Persisted brightness limits for all outputs. Plain object so it serializes cleanly.
/// The defaults (0 / 100 / 0 / 100 / 0 ms) keep the output exactly as it was without limits.
/// </summary>
public sealed class LightingLimitsSettingsData
{
    // Lamps that showed a cue colour stay at this brightness instead of going black (until the cue is cleared).
    public int BaseBrightnessPercent { get; set; } = 0;
    public int MaxBrightnessPercent { get; set; } = 100;

    // Strobe: brightness of the flash and of the pause between two flashes.
    public int StrobeMinPercent { get; set; } = 0;
    public int StrobeMaxPercent { get; set; } = 100;

    // Fade towards the base brightness when a lamp goes dark. Lighting up stays instant.
    public int FadeOutMs { get; set; } = 0;

    public LightingLimitsSettingsData Clone() => (LightingLimitsSettingsData)MemberwiseClone();

    public LightingLimitsSettingsData Normalized()
    {
        var copy = Clone();
        copy.MaxBrightnessPercent = Math.Clamp(MaxBrightnessPercent, 0, 100);
        copy.BaseBrightnessPercent = Math.Clamp(BaseBrightnessPercent, 0, copy.MaxBrightnessPercent);
        copy.StrobeMaxPercent = Math.Clamp(StrobeMaxPercent, 0, 100);
        copy.StrobeMinPercent = Math.Clamp(StrobeMinPercent, 0, copy.StrobeMaxPercent);
        copy.FadeOutMs = Math.Clamp(FadeOutMs, 0, 1000);
        return copy;
    }
}

/// <summary>
/// The active brightness limits, shared by the outputs. Set from the UI, read from any thread.
/// </summary>
public static class LightingLimits
{
    private static volatile LightingLimitsSettingsData _current = new();

    /// <summary>Raised after the limits changed, so outputs can re-render their current state.</summary>
    public static event Action? Changed;

    public static double BaseBrightness => _current.BaseBrightnessPercent / 100.0;
    public static double MaxBrightness => _current.MaxBrightnessPercent / 100.0;
    public static double StrobeMin => _current.StrobeMinPercent / 100.0;
    public static double StrobeMax => _current.StrobeMaxPercent / 100.0;
    public static TimeSpan FadeOut => TimeSpan.FromMilliseconds(_current.FadeOutMs);

    public static void Apply(LightingLimitsSettingsData data)
    {
        _current = data.Normalized();
        Changed?.Invoke();
    }
}
