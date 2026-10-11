using System;
using ReactiveUI;
using YALCY.Integrations;

namespace YALCY.ViewModels;

public partial class MainWindowViewModel
{
    private LightingLimitsSettingsData _lightingLimits = new();

    private void InitializeLighting()
    {
        _lightingLimits = SettingsManager.LightingLimits.Normalized();
        LightingLimits.Apply(_lightingLimits);
    }

    public LightingLimitsSettingsData GetLightingLimits() => _lightingLimits.Clone();

    private bool _lightingTestShowRunning;

    /// <summary>Runs the endless test show (chase + strobe every 5 s) on all enabled outputs.</summary>
    public bool LightingTestShowRunning
    {
        get => _lightingTestShowRunning;
        set
        {
            this.RaiseAndSetIfChanged(ref _lightingTestShowRunning, value);
            if (value)
            {
                LightingTestShow.Start();
            }
            else
            {
                LightingTestShow.Stop();
            }
        }
    }

    private void UpdateLightingLimits(Action<LightingLimitsSettingsData> change)
    {
        change(_lightingLimits);
        _lightingLimits = _lightingLimits.Normalized();

        // A lower limit can push an upper one (and the other way round), so refresh all of them.
        this.RaisePropertyChanged(nameof(LightingBaseBrightness));
        this.RaisePropertyChanged(nameof(LightingMaxBrightness));
        this.RaisePropertyChanged(nameof(LightingStrobeMin));
        this.RaisePropertyChanged(nameof(LightingStrobeMax));
        this.RaisePropertyChanged(nameof(LightingFadeOutMs));

        LightingLimits.Apply(_lightingLimits);
    }

    private static int ToPercent(double value) => (int)Math.Clamp(Math.Round(value), 0, 100);

    /// <summary>Brightness (0–100 %) a lamp keeps in its last cue colour instead of going black.</summary>
    public double LightingBaseBrightness
    {
        get => _lightingLimits.BaseBrightnessPercent;
        set
        {
            var percent = ToPercent(value);
            if (percent == _lightingLimits.BaseBrightnessPercent)
            {
                return;
            }

            UpdateLightingLimits(s =>
            {
                s.BaseBrightnessPercent = percent;
                s.MaxBrightnessPercent = Math.Max(s.MaxBrightnessPercent, percent);
            });
        }
    }

    /// <summary>Brightness (0–100 %) of a lit lamp.</summary>
    public double LightingMaxBrightness
    {
        get => _lightingLimits.MaxBrightnessPercent;
        set
        {
            var percent = ToPercent(value);
            if (percent == _lightingLimits.MaxBrightnessPercent)
            {
                return;
            }

            UpdateLightingLimits(s =>
            {
                s.MaxBrightnessPercent = percent;
                s.BaseBrightnessPercent = Math.Min(s.BaseBrightnessPercent, percent);
            });
        }
    }

    /// <summary>Brightness (0–100 %) of the pause between two strobe flashes.</summary>
    public double LightingStrobeMin
    {
        get => _lightingLimits.StrobeMinPercent;
        set
        {
            var percent = ToPercent(value);
            if (percent == _lightingLimits.StrobeMinPercent)
            {
                return;
            }

            UpdateLightingLimits(s =>
            {
                s.StrobeMinPercent = percent;
                s.StrobeMaxPercent = Math.Max(s.StrobeMaxPercent, percent);
            });
        }
    }

    /// <summary>Brightness (0–100 %) of a strobe flash.</summary>
    public double LightingStrobeMax
    {
        get => _lightingLimits.StrobeMaxPercent;
        set
        {
            var percent = ToPercent(value);
            if (percent == _lightingLimits.StrobeMaxPercent)
            {
                return;
            }

            UpdateLightingLimits(s =>
            {
                s.StrobeMaxPercent = percent;
                s.StrobeMinPercent = Math.Min(s.StrobeMinPercent, percent);
            });
        }
    }

    /// <summary>Fade time (ms) when a lamp goes dark.</summary>
    public double LightingFadeOutMs
    {
        get => _lightingLimits.FadeOutMs;
        set
        {
            // Snap to 10 ms steps; finer steps aren't visible on the lamps.
            var ms = (int)Math.Clamp(Math.Round(value / 10) * 10, 0, 1000);
            if (ms == _lightingLimits.FadeOutMs)
            {
                return;
            }

            UpdateLightingLimits(s => s.FadeOutMs = ms);
        }
    }
}
