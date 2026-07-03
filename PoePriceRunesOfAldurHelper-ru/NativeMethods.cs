using System.Runtime.InteropServices;

namespace PoePriceRunesOfAldurHelperRu;

internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;
    public const int MOD_NONE = 0x0000;
    public const int VK_F6 = 0x75;
    public const int VK_F7 = 0x76;
    public const int VK_F8 = 0x77;
    public const int HOTKEY_F6_ID = 3;
    public const int HOTKEY_F7_ID = 1;
    public const int HOTKEY_F8_ID = 2;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
