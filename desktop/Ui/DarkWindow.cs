using System.Runtime.InteropServices;

namespace AceHeadsetFix.Ui;

public static class DarkWindow
{
    const int UseImmersiveDarkMode = 20;
    const int UseImmersiveDarkModeBefore20H1 = 19;

    public static void ApplyToTitleBar(IntPtr handle)
    {
        var enabled = 1;
        if (DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
            DwmSetWindowAttribute(handle, UseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
    }

    public static void ApplyToScrollBars(Control control)
    {
        control.HandleCreated += (_, _) => SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
        if (control.IsHandleCreated)
            SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);
}
