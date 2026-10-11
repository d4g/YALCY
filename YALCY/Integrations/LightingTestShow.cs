using System;
using System.Threading;
using System.Threading.Tasks;
using YALCY.Integrations.StageKit;
using YALCY.Usb;

namespace YALCY.Integrations;

/// <summary>
/// A simple endless show for tuning the lighting without YARG: a chase runs around the 8 Stage Kit
/// positions and changes colour every round, and every 5 seconds there is a short strobe burst.
/// Goes to every enabled output, like real cues.
/// </summary>
public static class LightingTestShow
{
    private const int StepMs = 250;
    private const int StrobeEveryMs = 5000;
    private const int StrobeBurstMs = 1500;

    private static readonly object Lock = new();
    private static CancellationTokenSource? _cancellationTokenSource;

    private static readonly StageKitTalker.CommandId[] Colors =
    {
        StageKitTalker.CommandId.BlueLeds,
        StageKitTalker.CommandId.GreenLeds,
        StageKitTalker.CommandId.YellowLeds,
        StageKitTalker.CommandId.RedLeds
    };

    /// <summary>While true, outputs let the test strobes through the safety blackout (no data from YARG).</summary>
    public static bool IsRunning { get; private set; }

    public static void Start()
    {
        CancellationTokenSource cancellationTokenSource;
        lock (Lock)
        {
            if (_cancellationTokenSource != null)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource = cancellationTokenSource;
            IsRunning = true;
        }

        _ = RunAsync(cancellationTokenSource.Token);
    }

    public static void Stop()
    {
        CancellationTokenSource? cancellationTokenSource;
        lock (Lock)
        {
            cancellationTokenSource = _cancellationTokenSource;
            _cancellationTokenSource = null;
        }

        if (cancellationTokenSource == null)
        {
            return;
        }

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }

    private static async Task RunAsync(CancellationToken token)
    {
        var elapsedMs = 0;
        var strobeOn = false;
        var step = 0;

        try
        {
            while (!token.IsCancellationRequested)
            {
                // Chase: two opposite positions lit, one colour per round, the other colours dark.
                var position = step % 8;
                var color = Colors[step / 8 % Colors.Length];
                var mask = (byte)((1 << position) | (1 << ((position + 4) % 8)));

                foreach (var other in Colors)
                {
                    Send(other, other == color ? mask : (byte)0);
                }

                var phaseMs = elapsedMs % StrobeEveryMs;
                if (!strobeOn && elapsedMs > 0 && phaseMs < StepMs)
                {
                    Send(StageKitTalker.CommandId.StrobeSlow, 0);
                    strobeOn = true;
                }
                else if (strobeOn && phaseMs >= StrobeBurstMs)
                {
                    Send(StageKitTalker.CommandId.StrobeOff, 0);
                    strobeOn = false;
                }

                await Task.Delay(StepMs, token);
                elapsedMs += StepMs;
                step++;
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lighting test show error: {ex.Message}");
        }
        finally
        {
            Send(StageKitTalker.CommandId.StrobeOff, 0);
            Send(StageKitTalker.CommandId.DisableAll, 0);
            lock (Lock)
            {
                // A quick stop + start already began a new run; don't mark that one as stopped.
                if (_cancellationTokenSource == null)
                {
                    IsRunning = false;
                }
            }
        }
    }

    private static void Send(StageKitTalker.CommandId commandId, byte parameter)
    {
        UsbDeviceMonitor.SimulateStageKitCommand(commandId, parameter);
    }
}
