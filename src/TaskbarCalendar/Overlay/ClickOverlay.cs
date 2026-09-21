using System.Runtime.InteropServices;
using TaskbarCalendar.Interop;
using TaskbarCalendar.Services;

namespace TaskbarCalendar.Overlay;

/// <summary>
/// 覆盖在任务栏时钟上的隐形点击层（纯 Win32 窗口实现）。
/// 不渲染任何内容，透明度为 1/255（肉眼不可见，同时保留鼠标命中），仅拦截左键与右键点击。
/// </summary>
public sealed class ClickOverlay : IDisposable
{
    private const string WindowClassName = "TaskbarCalendarOverlayWnd";
    private const byte OverlayAlpha = 1;

    private NativeMethods.WndProcDelegate? _wndProcDelegate;
    private IntPtr _hwnd = IntPtr.Zero;
    private IntPtr _arrowCursor = IntPtr.Zero;
    private bool _classRegistered;
    private bool _visible;

    /// <summary>左键点击时钟</summary>
    public event EventHandler? LeftClicked;

    /// <summary>右键点击时钟</summary>
    public event EventHandler? RightClicked;

    public void Create()
    {
        if (_hwnd != IntPtr.Zero)
        {
            return;
        }

        IntPtr hInstance = NativeMethods.GetModuleHandle(null);

        // 保持委托引用存活，避免窗口过程被 GC 回收
        _wndProcDelegate = WndProc;

        // 类光标设为标准箭头，避免光标状态残留（如加载转圈）
        _arrowCursor = NativeMethods.LoadCursor(IntPtr.Zero, NativeMethods.IDC_ARROW);

        var wndClass = new NativeMethods.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = hInstance,
            hCursor = _arrowCursor,
            lpszClassName = WindowClassName,
        };

        if (NativeMethods.RegisterClassEx(ref wndClass) == 0)
        {
            Logger.Error($"注册点击层窗口类失败，错误码 {Marshal.GetLastWin32Error()}");
            return;
        }

        _classRegistered = true;

        _hwnd = NativeMethods.CreateWindowEx(
            NativeMethods.WS_EX_TOPMOST
                | NativeMethods.WS_EX_TOOLWINDOW
                | NativeMethods.WS_EX_LAYERED
                | NativeMethods.WS_EX_NOACTIVATE,
            WindowClassName,
            "TaskbarCalendarOverlay",
            NativeMethods.WS_POPUP,
            0,
            0,
            1,
            1,
            IntPtr.Zero,
            IntPtr.Zero,
            hInstance,
            IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            Logger.Error($"创建点击层窗口失败，错误码 {Marshal.GetLastWin32Error()}");
            return;
        }

        // alpha = 1/255：肉眼不可见，同时保留鼠标命中（alpha 为 0 会穿透点击）
        if (!NativeMethods.SetLayeredWindowAttributes(_hwnd, 0, OverlayAlpha, NativeMethods.LWA_ALPHA))
        {
            Logger.Warn($"设置点击层透明失败，错误码 {Marshal.GetLastWin32Error()}");
        }

        NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_SHOWNOACTIVATE);
        Logger.Info("隐形点击层已创建（纯 Win32）");
    }

    /// <summary>移动到时钟位置并保持置顶；rect 为 null 时隐藏</summary>
    public void Update(ClockRect? rect)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        if (rect is null)
        {
            NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_HIDE);
            _visible = false;
            return;
        }

        ClockRect r = rect.Value;
        _visible = true;
        NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_SHOWNOACTIVATE);
        NativeMethods.SetWindowPos(
            _hwnd,
            NativeMethods.HWND_TOPMOST,
            r.Left,
            r.Top,
            r.Width,
            r.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    /// <summary>保持点击层位于任务栏之上（建议定期调用，防御 explorer 重排 z-order）</summary>
    public void BringToTop()
    {
        if (_hwnd == IntPtr.Zero || !_visible)
        {
            return;
        }

        NativeMethods.SetWindowPos(
            _hwnd,
            NativeMethods.HWND_TOPMOST,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case NativeMethods.WM_SETCURSOR:
                // 显式设置箭头光标，确保不残留进入前的光标状态（如加载转圈）
                NativeMethods.SetCursor(_arrowCursor);
                return new IntPtr(1);

            case NativeMethods.WM_MOUSEACTIVATE:
                // 点击不激活本窗口，避免抢走前台焦点
                return new IntPtr(NativeMethods.MA_NOACTIVATE);

            case NativeMethods.WM_LBUTTONUP:
                LeftClicked?.Invoke(this, EventArgs.Empty);
                return IntPtr.Zero;

            case NativeMethods.WM_RBUTTONUP:
                RightClicked?.Invoke(this, EventArgs.Empty);
                return IntPtr.Zero;

            case NativeMethods.WM_DESTROY:
                return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }

        if (_classRegistered)
        {
            NativeMethods.UnregisterClass(WindowClassName, NativeMethods.GetModuleHandle(null));
            _classRegistered = false;
        }

        _wndProcDelegate = null;
    }
}
