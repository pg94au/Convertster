using System.Runtime.InteropServices;

namespace ImageConverter;

internal static class ProgressBarExtensions
{
    private const int PbmSetstate = 0x0410;
    private const int PbstNormal = 0x0001;
    private const int PbstError = 0x0002;
    private const int PbstPaused = 0x0003;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);

    public static void SetState(this ProgressBar bar, ProgressBarState state)
    {
        SendMessage(bar.Handle, PbmSetstate, (IntPtr)state, IntPtr.Zero);
    }

    public enum ProgressBarState
    {
        Normal = PbstNormal,
        Error = PbstError,
        Paused = PbstPaused
    }
}

