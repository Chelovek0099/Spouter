using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Soundpad;

public sealed class PadViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly AudioPadPlayer _player = new();

    private string _name;
    private string? _filePath;
    private float _volume = 1.0f;
    private Key _hotkey;
    private bool _isPlaying;
    private string? _deviceId;

    public PadViewModel(string name, Key hotkey)
    {
        _name = name;
        _hotkey = hotkey;
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string? FilePath
    {
        get => _filePath;
        set => SetField(ref _filePath, value);
    }

    public float Volume
    {
        get => _volume;
        set
        {
            if (SetField(ref _volume, Math.Clamp(value, 0f, 1f)))
            {
                _player.Volume = _volume;
            }
        }
    }

    public Key Hotkey
    {
        get => _hotkey;
        set => SetField(ref _hotkey, value);
    }

    public string? DeviceId
    {
        get => _deviceId;
        set
        {
            if (SetField(ref _deviceId, value))
            {
                _player.DeviceId = value;
                // При смене устройства вывода пересоздаем плеер для уже выбранного файла,
                // чтобы следующее воспроизведение сразу шло в новое устройство.
                if (!string.IsNullOrEmpty(FilePath))
                {
                    _player.Load(FilePath);
                }
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set => SetField(ref _isPlaying, value);
    }

    public void LoadFile(string path)
    {
        _player.Load(path);
        FilePath = path;
        IsPlaying = false;
    }

    public void TogglePlay()
    {
        if (!_player.IsLoaded && !string.IsNullOrEmpty(FilePath))
        {
            _player.Load(FilePath);
        }
        _player.TogglePlay();
        IsPlaying = _player.IsPlaying;
    }

    public void Stop()
    {
        _player.Stop();
        IsPlaying = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public void Dispose()
    {
        _player.Dispose();
    }
}

