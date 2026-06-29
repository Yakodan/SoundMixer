using SoundMixer.Core.DSP;
using SoundMixer.Core.Models;
using SoundMixer.Core.Soundboard;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.IO;

namespace SoundMixer.Core.Audio;

public class AudioEngine : IDisposable
{
    private readonly AudioDeviceManager _deviceManager;
    private readonly MicrophoneCapture _micCapture;
    private readonly VirtualOutputWriter _virtualOutput;
    private SoundboardManager _soundboard;

    private readonly GainProcessor _gain;
    private readonly NoiseGateProcessor _noiseGate;
    private readonly EqualizerProcessor _equalizer;
    private readonly CompressorProcessor _compressor;
    private readonly NoiseSuppressionProcessor _noiseSuppression;
    private readonly List<IDSPProcessor> _dspChain;

    private WasapiOut? _monitorOutput;
    private BufferedWaveProvider? _monitorBuffer;

    private int _actualSampleRate;
    private int _actualChannels;

    private float[]? _processingBuffer;
    private float[]? _soundboardBuffer;
    private float[]? _mixedBuffer;
    private float[]? _monitorBufferData;
    private byte[]? _monitorByteBuffer;

    private AudioSettings _settings;
    private bool _isRunning;

    public event EventHandler<float>? InputLevelChanged;
    public event EventHandler<float>? OutputLevelChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<string>? ClipLoadErrorOccurred;
    public event EventHandler<bool>? RunningStateChanged;
    public event EventHandler? SoundboardReinitialized;

    public AudioDeviceManager DeviceManager => _deviceManager;
    public SoundboardManager Soundboard => _soundboard;
    public EqualizerProcessor Equalizer => _equalizer;
    public NoiseGateProcessor NoiseGate => _noiseGate;
    public CompressorProcessor Compressor => _compressor;
    public NoiseSuppressionProcessor NoiseSuppression => _noiseSuppression;
    public GainProcessor Gain => _gain;
    public AudioSettings Settings => _settings;
    public bool IsRunning => _isRunning;

    public AudioEngine()
    {
        _deviceManager = new AudioDeviceManager();
        _micCapture = new MicrophoneCapture(_deviceManager);
        _virtualOutput = new VirtualOutputWriter(_deviceManager);

        var settingsPath = GetSettingsPath();

        System.Diagnostics.Debug.WriteLine($"[AudioEngine] Settings: {settingsPath}");
        System.Diagnostics.Debug.WriteLine($"[AudioEngine] Exists: {File.Exists(settingsPath)}");

        _settings = AudioSettings.Load(settingsPath);

        System.Diagnostics.Debug.WriteLine($"[AudioEngine] Clips in settings: {_settings.SoundClips.Count}");

        _actualSampleRate = _settings.SampleRate;
        _actualChannels = _settings.Channels;

        _soundboard = new SoundboardManager(_actualSampleRate, _actualChannels);
        _soundboard.ClipLoadError += (s, msg) =>
        {
            System.Diagnostics.Debug.WriteLine($"[AudioEngine] CLIP ERROR: {msg}");
            ClipLoadErrorOccurred?.Invoke(this, msg);
        };

        _gain = new GainProcessor { Order = 0 };
        _noiseGate = new NoiseGateProcessor { Order = 1, IsEnabled = _settings.NoiseGateEnabled, ThresholdDb = _settings.NoiseGateThreshold };
        _noiseSuppression = new NoiseSuppressionProcessor { Order = 2 };
        _equalizer = new EqualizerProcessor { Order = 3 };
        _compressor = new CompressorProcessor { Order = 4, IsEnabled = _settings.CompressorEnabled, ThresholdDb = _settings.CompressorThreshold, Ratio = _settings.CompressorRatio };

        _dspChain = new List<IDSPProcessor> { _gain, _noiseGate, _noiseSuppression, _equalizer, _compressor };

        _soundboard.LoadFromSettings(_settings.SoundClips);

        System.Diagnostics.Debug.WriteLine($"[AudioEngine] Loaded into soundboard: {_soundboard.Clips.Count}");

        if (_settings.EqualizerBands.Length > 0)
            _equalizer.SetAllBands(_settings.EqualizerBands);
    }

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            if (string.IsNullOrEmpty(_settings.InputDeviceId))
                throw new InvalidOperationException("Микрофон не выбран!");
            if (string.IsNullOrEmpty(_settings.VirtualCableDeviceId))
                throw new InvalidOperationException("Виртуальный кабель не выбран!");

            _processingBuffer = null;

            _micCapture.DataAvailable += OnMicDataAvailable;
            _micCapture.LevelChanged += (s, level) => InputLevelChanged?.Invoke(this, level);
            _micCapture.StartWithWasapiFormat(_settings.InputDeviceId);

            var captureFormat = _micCapture.WaveFormat;
            int newSampleRate = captureFormat?.SampleRate ?? _settings.SampleRate;
            int newChannels = captureFormat?.Channels ?? _settings.Channels;

            if (newSampleRate != _actualSampleRate || newChannels != _actualChannels)
            {
                _actualSampleRate = newSampleRate;
                _actualChannels = newChannels;
                ReinitializeSoundboard();
            }
            else
            {
                _actualSampleRate = newSampleRate;
                _actualChannels = newChannels;
            }

            _virtualOutput.Start(_settings.VirtualCableDeviceId, _actualSampleRate, _actualChannels);

            if (_settings.MonitorEnabled && !string.IsNullOrEmpty(_settings.OutputDeviceId))
                StartMonitoring(_settings.OutputDeviceId, _actualSampleRate, _actualChannels);

            _isRunning = true;
            RunningStateChanged?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            Stop();
        }
    }

    private void ReinitializeSoundboard()
    {
        var clipInfos = _soundboard.GetClipInfos();
        _soundboard.Dispose();
        _soundboard = new SoundboardManager(_actualSampleRate, _actualChannels);
        _soundboard.ClipLoadError += (s, msg) => ClipLoadErrorOccurred?.Invoke(this, msg);
        _soundboard.LoadFromSettings(clipInfos);
        SoundboardReinitialized?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        _micCapture.DataAvailable -= OnMicDataAvailable;
        _micCapture.Stop();
        _virtualOutput.Stop();
        StopMonitoring();
        _soundboard.StopAll();
        _isRunning = false;
        RunningStateChanged?.Invoke(this, false);
    }

    private void OnMicDataAvailable(object? sender, float[] micData)
    {
        try
        {
            int count = micData.Length;

            if (_processingBuffer == null || _processingBuffer.Length != count)
            {
                _processingBuffer = new float[count];
                _soundboardBuffer = new float[count];
                _mixedBuffer = new float[count];
                _monitorBufferData = new float[count];
                _monitorByteBuffer = new byte[count * 4];
            }

            Array.Copy(micData, _processingBuffer, count);
            AudioMixer.ApplyVolume(_processingBuffer, _settings.MicrophoneVolume, count);

            foreach (var processor in _dspChain.OrderBy(p => p.Order))
            {
                if (processor.IsEnabled)
                    processor.Process(_processingBuffer, 0, count, _actualSampleRate, _actualChannels);
            }

            Array.Clear(_soundboardBuffer!, 0, count);
            _soundboard.ReadMixed(_soundboardBuffer!, 0, count);
            const float SoundboardAttenuation = 0.10f;
            AudioMixer.ApplyVolume(_soundboardBuffer!, _settings.SoundboardVolume * SoundboardAttenuation, count);

            AudioMixer.Mix(_processingBuffer, 1f, _soundboardBuffer!, 1f, _mixedBuffer!, count);
            _virtualOutput.WriteSamples(_mixedBuffer!, 0, count);

            if (_settings.MonitorEnabled && _monitorBuffer != null)
            {
                Array.Clear(_monitorBufferData!, 0, count);
                if (_settings.MonitorMicrophone)
                    AudioMixer.MixInto(_monitorBufferData!, _processingBuffer, 1f, count);
                if (_settings.MonitorSoundboard)
                    AudioMixer.MixInto(_monitorBufferData!, _soundboardBuffer!, 1f, count);
                AudioMixer.ApplyVolume(_monitorBufferData!, _settings.MonitorVolume, count);
                Buffer.BlockCopy(_monitorBufferData!, 0, _monitorByteBuffer!, 0, count * 4);
                try { _monitorBuffer.AddSamples(_monitorByteBuffer!, 0, count * 4); } catch { }
            }

            float maxLevel = 0;
            for (int i = 0; i < count; i++)
            {
                float abs = Math.Abs(_mixedBuffer![i]);
                if (abs > maxLevel) maxLevel = abs;
            }
            OutputLevelChanged?.Invoke(this, maxLevel);
        }
        catch { }
    }

    private void StartMonitoring(string deviceId, int sampleRate, int channels)
    {
        StopMonitoring();
        try
        {
            var device = _deviceManager.GetDeviceById(deviceId);
            if (device == null) return;
            var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            _monitorBuffer = new BufferedWaveProvider(format) { BufferLength = sampleRate * channels * 4 * 2, DiscardOnBufferOverflow = true };
            _monitorOutput = new WasapiOut(device, AudioClientShareMode.Shared, true, 10);
            _monitorOutput.Init(_monitorBuffer);
            _monitorOutput.Play();
        }
        catch { }
    }

    private void StopMonitoring()
    {
        if (_monitorOutput != null)
        {
            try { _monitorOutput.Stop(); } catch { }
            _monitorOutput.Dispose();
            _monitorOutput = null;
        }
        _monitorBuffer = null;
    }

    /// <summary>
    /// Сохраняет ВСЕ настройки включая список клипов на диск.
    /// Вызывайте после любого изменения!
    /// </summary>
    public void SaveSettings()
    {
        _settings.SoundClips = _soundboard.GetClipInfos();
        _settings.EqualizerBands = _equalizer.Bands.Select(b => b.GainDb).ToArray();
        _settings.NoiseGateEnabled = _noiseGate.IsEnabled;
        _settings.NoiseGateThreshold = _noiseGate.ThresholdDb;
        _settings.CompressorEnabled = _compressor.IsEnabled;
        _settings.CompressorThreshold = _compressor.ThresholdDb;
        _settings.CompressorRatio = _compressor.Ratio;

        var path = GetSettingsPath();
        _settings.Save(path);

        // Верификация
        if (File.Exists(path))
        {
            var saved = File.ReadAllText(path);
            System.Diagnostics.Debug.WriteLine($"[AudioEngine] SAVED {_settings.SoundClips.Count} clips to {path}");
            System.Diagnostics.Debug.WriteLine($"[AudioEngine] Verify file size: {saved.Length} bytes");

            // Проверяем что клипы реально записались
            var verify = AudioSettings.Load(path);
            System.Diagnostics.Debug.WriteLine($"[AudioEngine] Verify reload: {verify.SoundClips.Count} clips");
        }
    }

    public void UpdateSettings(AudioSettings newSettings)
    {
        bool wasRunning = _isRunning;
        bool needRestart = _settings.InputDeviceId != newSettings.InputDeviceId || _settings.VirtualCableDeviceId != newSettings.VirtualCableDeviceId || _settings.OutputDeviceId != newSettings.OutputDeviceId;
        _settings = newSettings;
        _noiseGate.IsEnabled = newSettings.NoiseGateEnabled;
        _noiseGate.ThresholdDb = newSettings.NoiseGateThreshold;
        _compressor.IsEnabled = newSettings.CompressorEnabled;
        _compressor.ThresholdDb = newSettings.CompressorThreshold;
        _compressor.Ratio = newSettings.CompressorRatio;
        _equalizer.SetAllBands(newSettings.EqualizerBands);
        if (needRestart && wasRunning) { Stop(); Start(); }
    }

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "SoundMixer", "settings.json");
    }

    public void Dispose()
    {
        Stop();
        // НЕ вызываем SaveSettings — это делает MainViewModel.ForceSave ДО Dispose
        _soundboard.Dispose();
        _deviceManager.Dispose();
    }
}