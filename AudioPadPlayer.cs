using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Soundpad;

public sealed class AudioPadPlayer : IDisposable
{
    private readonly object _lock = new();

    private IWavePlayer? _output;
    private AudioFileReader? _reader;
    private MMDevice? _device;

    public string? DeviceId { get; set; }

    public bool IsLoaded => _reader is not null;
    public bool IsPlaying { get; private set; }
    public string? FilePath { get; private set; }

    private float _volume = 1.0f;
    public float Volume
    {
        get => _volume;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            _volume = clamped;
            if (_reader is not null)
            {
                _reader.Volume = clamped;
            }
        }
    }

    public void Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is empty", nameof(path));

        lock (_lock)
        {
            StopInternal();
            ReleaseResources();

            try
            {
                FilePath = path;
                _reader = new AudioFileReader(path)
                {
                    Volume = _volume
                };

                var enumerator = new MMDeviceEnumerator();

                // Если устройство не выбрано явно — используем дефолтное.
                _device = string.IsNullOrWhiteSpace(DeviceId)
                    ? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                    : enumerator.GetDevice(DeviceId);

                if (_device.DataFlow != DataFlow.Render)
                {
                    throw new InvalidOperationException("Выбранное устройство не поддерживает вывод (DataFlow != Render).");
                }

                _output = new WasapiOut(_device, AudioClientShareMode.Shared, false, 200);
                _output.Init(_reader);
                _output.PlaybackStopped += OnPlaybackStopped;
            }
            catch
            {
                ReleaseResources();
                throw;
            }
        }
    }

    public void Play()
    {
        lock (_lock)
        {
            if (_reader is null || _output is null)
                return;

            _reader.Position = 0;
            _output.Play();
            IsPlaying = true;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            StopInternal();
        }
    }

    public void TogglePlay()
    {
        // Для простоты: всегда перезапускаем трек с начала,
        // независимо от текущего состояния.
        Play();
    }

    private void StopInternal()
    {
        if (_output is null || _reader is null)
            return;

        _output.Stop();
        _reader.Position = 0;
        IsPlaying = false;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        lock (_lock)
        {
            IsPlaying = false;
        }
    }

    private void ReleaseResources()
    {
        if (_output is not null)
        {
            _output.PlaybackStopped -= OnPlaybackStopped;
            _output.Dispose();
            _output = null;
        }

        _reader?.Dispose();
        _reader = null;

        _device?.Dispose();
        _device = null;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            StopInternal();
            ReleaseResources();
        }
    }
}
