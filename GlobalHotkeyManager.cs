using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Soundpad;

public sealed class GlobalHotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    private const uint MOD_NONE = 0x0000;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

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

    public void RegisterBindings(IEnumerable<(ModifierKeys modifiers, Key key, Action action)> bindings)
    {
        Clear();

        foreach (var (mod, key, action) in bindings)
        {
            var id = _nextId++;
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

            uint modifiers = 0;
            if (mod.HasFlag(ModifierKeys.Alt)) modifiers |= MOD_ALT;
            if (mod.HasFlag(ModifierKeys.Control)) modifiers |= MOD_CONTROL;
            if (mod.HasFlag(ModifierKeys.Shift)) modifiers |= MOD_SHIFT;
            if (mod.HasFlag(ModifierKeys.Windows)) modifiers |= MOD_WIN;

            if (!RegisterHotKey(_hwnd, id, modifiers, vk))
            {
                // Узнаем реальную причину ошибки
                int error = Marshal.GetLastWin32Error();
                MessageBox.Show($"Не удалось зарегистрировать клавишу {key}. Код ошибки: {error}\nВозможно, она занята другой программой.");
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

