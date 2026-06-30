namespace TabView.Avalonia;

internal static class Extensions
{
    extension(Window? window)
    {
        public void RestoreWindow()
        {
            if (window is null)
                return;
            if (!window.CanResize)
                return;
            if (window.WindowState is WindowState.FullScreen)
                return;
            window.WindowState = window.WindowState is WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}
