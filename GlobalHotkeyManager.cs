using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace Soundpad;

public sealed class GlobalHotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 1;

    public GlobalHotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public void RegisterBindings(IEnumerable<(Key key, Action action)> bindings)
    {
        Clear();

        foreach (var (key, action) in bindings)
        {
            var id = _nextId++;
            // Используем сочетание Ctrl+Alt+Key, чтобы не перехватывать
            // одиночные клавиши, необходимые другим программам.
            uint modifiers = MOD_CONTROL | MOD_ALT;
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

            if (!RegisterHotKey(_hwnd, id, modifiers, vk))
            {
                continue;
            }

            _handlers[id] = action;
        }
    }

    public void ProcessMessage(IntPtr wParam, IntPtr lParam)
    {
        var id = wParam.ToInt32();
        if (_handlers.TryGetValue(id, out var handler))
        {
            handler();
        }
    }

    public void Clear()
    {
        foreach (var id in _handlers.Keys)
        {
            UnregisterHotKey(_hwnd, id);
        }

        _handlers.Clear();
        _nextId = 1;
    }

    public void Dispose()
    {
        Clear();
    }

    public static IntPtr Hook(IntPtr hwnd, IntPtr wParam, IntPtr lParam, ref bool handled, GlobalHotkeyManager manager, int msg)
    {
        if (msg == WM_HOTKEY)
        {
            manager.ProcessMessage(wParam, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }
}

