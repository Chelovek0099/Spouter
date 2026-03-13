using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using NAudio.CoreAudioApi;

namespace Soundpad;

public sealed class MainViewModel : IDisposable
{
    public ObservableCollection<PadViewModel> Pads { get; } = new();
    public ObservableCollection<AudioDeviceItem> OutputDevices { get; } = new();

    private AudioDeviceItem? _selectedOutputDevice;

    public AudioDeviceItem? SelectedOutputDevice
    {
        get => _selectedOutputDevice;
        set
        {
            if (_selectedOutputDevice == value)
                return;

            _selectedOutputDevice = value;
            var id = _selectedOutputDevice?.Id;

            foreach (var pad in Pads)
            {
                pad.DeviceId = id;
            }
        }
    }

    public MainViewModel()
    {
        var keys = new[]
        {
            Key.F1, Key.F2, Key.F3, Key.F4,
            Key.F5, Key.F6, Key.F7, Key.F8
        };

        for (int i = 0; i < keys.Length; i++)
        {
            Pads.Add(new PadViewModel($"Pad {i + 1}", keys[i]));
        }

        LoadOutputDevices();
    }

    public PadViewModel? FindByKey(Key key)
    {
        return Pads.FirstOrDefault(p => p.Hotkey == key);
    }

    public void Dispose()
    {
        foreach (var pad in Pads)
        {
            pad.Dispose();
        }
    }

    public void ApplySettings(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OutputDeviceId))
        {
            var device = OutputDevices.FirstOrDefault(d => d.Id == settings.OutputDeviceId);
            if (device is not null)
            {
                SelectedOutputDevice = device;
            }
        }

        for (int i = 0; i < Pads.Count && i < settings.Pads.Count; i++)
        {
            var pad = Pads[i];
            var s = settings.Pads[i];

            pad.FilePath = s.FilePath;
            pad.Volume = s.Volume;

            if (Enum.TryParse<Key>(s.Hotkey, out var key))
            {
                pad.Hotkey = key;
            }
        }
    }

    public AppSettings CreateSettingsSnapshot()
    {
        var settings = new AppSettings
        {
            OutputDeviceId = SelectedOutputDevice?.Id
        };

        foreach (var pad in Pads)
        {
            settings.Pads.Add(new PadSettings
            {
                FilePath = pad.FilePath,
                Volume = pad.Volume,
                Hotkey = pad.Hotkey.ToString()
            });
        }

        return settings;
    }

    private void LoadOutputDevices()
    {
        var enumerator = new MMDeviceEnumerator();

        // Только устройства вывода (Render) — для воспроизведения и виртуальных микрофонов-кабелей.
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        OutputDevices.Clear();

        AudioDeviceItem? defaultItem = null;
        foreach (var dev in devices)
        {
            var item = new AudioDeviceItem(dev.ID, dev.FriendlyName);
            OutputDevices.Add(item);

            if (dev.ID == enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID)
            {
                defaultItem = item;
            }
        }

        if (defaultItem is null)
        {
            defaultItem = OutputDevices.FirstOrDefault();
        }

        SelectedOutputDevice = defaultItem;
    }
}

