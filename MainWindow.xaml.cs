using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
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

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Режим захвата хоткея: переназначаем клавишу для выбранного пэда.
        if (_hotkeyCaptureTarget is not null)
        {
            if (e.Key != Key.Escape && e.Key != Key.System)
            {
                _hotkeyCaptureTarget.Hotkey = e.Key;
            }

            _hotkeyCaptureTarget = null;
            e.Handled = true;
            return;
        }
    }

    private void SetHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not PadViewModel pad)
            return;

        _hotkeyCaptureTarget = pad;
        MessageBox.Show(this,
            "Нажмите новую клавишу для этого пэда (Esc — отмена).",
            "Переназначение хоткея",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
        if (_hotkeyManager is null)
            return;

        var bindings = _viewModel.Pads
            .Select(pad => (pad.Hotkey, (Action)(() => TogglePadFromHotkey(pad))));

        _hotkeyManager.RegisterBindings(bindings);
    }

    private void TogglePadFromHotkey(PadViewModel pad)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                pad.TogglePlay();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Ошибка при воспроизведении по глобальной горячей клавише:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        });
    }

    private void PadOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PadViewModel.Hotkey))
        {
            RegisterGlobalHotkeys();
        }
    }
}