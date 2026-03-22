using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Soundpad;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private PadViewModel? _hotkeyCaptureTarget;
    private HwndSource? _hwndSource;
    private GlobalHotkeyManager? _hotkeyManager;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        foreach (var pad in _viewModel.Pads)
        {
            pad.PropertyChanged += PadOnPropertyChanged;
        }

        // Загрузка настроек (звуки, громкости, хоткеи, устройство вывода)
        var settings = AppSettingsStore.Load();
        _viewModel.ApplySettings(settings);
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not PadViewModel pad)
            return;

        var dialog = new OpenFileDialog
        {
            Filter = "Audio files|*.wav;*.mp3;*.flac;*.aac;*.wma;*.m4a|All files|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            try
            {
                pad.LoadFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Не удалось загрузить файл:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not PadViewModel pad)
            return;

        try
        {
            pad.TogglePlay();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Ошибка при воспроизведении:\n{ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private bool IsModifierKey(Key key)
    {
        return key == Key.LeftCtrl || key == Key.RightCtrl ||
               key == Key.LeftAlt || key == Key.RightAlt ||
               key == Key.LeftShift || key == Key.RightShift ||
               key == Key.LWin || key == Key.RWin;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_hotkeyCaptureTarget is null) return;

        Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;
        if (IsModifierKey(key)) return;

        e.Handled = true;

        if (key == Key.Escape)
        {
            _hotkeyCaptureTarget = null;
            return;
        }

        _hotkeyCaptureTarget.Hotkey = key;
        _hotkeyCaptureTarget.HotkeyModifiers = Keyboard.Modifiers;

        _hotkeyCaptureTarget = null;
    }

    private void OnSetKeyClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var pad = button?.DataContext as PadViewModel;
        if (pad == null) return;

        // Создаем окно захвата
        var captureWindow = new HotkeyCaptureWindow();
        captureWindow.Owner = this; // Чтобы окно было по центру основного

        if (captureWindow.ShowDialog() == true && captureWindow.IsSuccess)
        {
            // Применяем новые данные
            pad.Hotkey = captureWindow.ResultKey;
            pad.HotkeyModifiers = captureWindow.ResultModifiers;

            // Принудительно вызываем перерегистрацию всех хоткеев
            RegisterGlobalHotkeys();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _hotkeyManager?.Dispose();
        _hwndSource?.RemoveHook(WndProc);
        var snapshot = _viewModel.CreateSettingsSnapshot();
        AppSettingsStore.Save(snapshot);
        _viewModel.Dispose();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var helper = new WindowInteropHelper(this);
        _hwndSource = HwndSource.FromHwnd(helper.Handle);
        if (_hwndSource is not null)
        {
            _hwndSource.AddHook(WndProc);
            _hotkeyManager = new GlobalHotkeyManager(helper.Handle);
            RegisterGlobalHotkeys();
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_hotkeyManager is not null)
        {
            GlobalHotkeyManager.Hook(hwnd, wParam, lParam, ref handled, _hotkeyManager, msg);
        }

        return IntPtr.Zero;
    }

    private void RegisterGlobalHotkeys()
    {
        if (_hotkeyManager is null) return;

        var bindings = _viewModel.Pads
            .Select(pad => (pad.HotkeyModifiers, pad.Hotkey, (Action)(() => TogglePadFromHotkey(pad))));

        _hotkeyManager.RegisterBindings(bindings);
    }

    private void TogglePadFromHotkey(PadViewModel pad)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                pad.TogglePlay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка хоткея: {ex.Message}");
            }
        }));
    }

    private void PadOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PadViewModel.Hotkey))
        {
            RegisterGlobalHotkeys();
        }
    }
}