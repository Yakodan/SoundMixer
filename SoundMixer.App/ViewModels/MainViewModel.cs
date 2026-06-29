using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using SoundMixer.Core.Audio;
using SoundMixer.Core.Models;
using SoundMixer.Core.Soundboard;
using SoundMixer.App.Helpers;

namespace SoundMixer.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly AudioEngine _engine;
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _autoSaveTimer;
    private readonly Dispatcher _dispatcher;
    private bool _disposed = false;

    public ObservableCollection<AudioDeviceInfo> InputDevices { get; } = new();
    public ObservableCollection<AudioDeviceInfo> OutputDevices { get; } = new();
    public ObservableCollection<AudioDeviceInfo> VirtualCableDevices { get; } = new();
    public ObservableCollection<SoundClipViewModel> SoundClips { get; } = new();
    public ObservableCollection<EqualizerBandViewModel> EqualizerBands { get; } = new();
    public ICommand TrimClipCommand { get; }

    private AudioDeviceInfo? _selectedInputDevice;
    public AudioDeviceInfo? SelectedInputDevice
    {
        get => _selectedInputDevice;
        set { _selectedInputDevice = value; if (value != null) _engine.Settings.InputDeviceId = value.Id; OnPropertyChanged(); }
    }

    private AudioDeviceInfo? _selectedOutputDevice;
    public AudioDeviceInfo? SelectedOutputDevice
    {
        get => _selectedOutputDevice;
        set { _selectedOutputDevice = value; if (value != null) _engine.Settings.OutputDeviceId = value.Id; OnPropertyChanged(); }
    }

    private AudioDeviceInfo? _selectedVirtualCable;
    public AudioDeviceInfo? SelectedVirtualCable
    {
        get => _selectedVirtualCable;
        set { _selectedVirtualCable = value; if (value != null) _engine.Settings.VirtualCableDeviceId = value.Id; OnPropertyChanged(); }
    }

    private float _inputLevel;
    public float InputLevel { get => _inputLevel; set { _inputLevel = value; OnPropertyChanged(); } }

    private float _outputLevel;
    public float OutputLevel { get => _outputLevel; set { _outputLevel = value; OnPropertyChanged(); } }

    public float MicrophoneVolume { get => _engine.Settings.MicrophoneVolume; set { _engine.Settings.MicrophoneVolume = value; OnPropertyChanged(); } }
    public float SoundboardVolume { get => _engine.Settings.SoundboardVolume; set { _engine.Settings.SoundboardVolume = value; OnPropertyChanged(); } }
    public float MonitorVolume { get => _engine.Settings.MonitorVolume; set { _engine.Settings.MonitorVolume = value; OnPropertyChanged(); } }
    public bool MonitorEnabled { get => _engine.Settings.MonitorEnabled; set { _engine.Settings.MonitorEnabled = value; OnPropertyChanged(); } }
    public bool MonitorMicrophone { get => _engine.Settings.MonitorMicrophone; set { _engine.Settings.MonitorMicrophone = value; OnPropertyChanged(); } }
    public bool MonitorSoundboard { get => _engine.Settings.MonitorSoundboard; set { _engine.Settings.MonitorSoundboard = value; OnPropertyChanged(); } }

    private bool _isRunning;
    public bool IsRunning { get => _isRunning; set { _isRunning = value; OnPropertyChanged(); OnPropertyChanged(nameof(StartStopButtonText)); } }
    public string StartStopButtonText => IsRunning ? "Stop" : "Start";

    private string _statusMessage = "Ready";
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

    public string StorageSize => SoundClipStorage.FormatSize(SoundClipStorage.GetTotalSize());

    public bool NoiseGateEnabled { get => _engine.NoiseGate.IsEnabled; set { _engine.NoiseGate.IsEnabled = value; _engine.Settings.NoiseGateEnabled = value; OnPropertyChanged(); } }
    public float NoiseGateThreshold { get => _engine.NoiseGate.ThresholdDb; set { _engine.NoiseGate.ThresholdDb = value; _engine.Settings.NoiseGateThreshold = value; OnPropertyChanged(); } }
    public bool CompressorEnabled { get => _engine.Compressor.IsEnabled; set { _engine.Compressor.IsEnabled = value; _engine.Settings.CompressorEnabled = value; OnPropertyChanged(); } }
    public float CompressorThreshold { get => _engine.Compressor.ThresholdDb; set { _engine.Compressor.ThresholdDb = value; _engine.Settings.CompressorThreshold = value; OnPropertyChanged(); } }
    public float CompressorRatio { get => _engine.Compressor.Ratio; set { _engine.Compressor.Ratio = value; _engine.Settings.CompressorRatio = value; OnPropertyChanged(); } }

    public List<string> AvailableThemes { get; } = ThemeManager.AvailableThemes.ToList();
    private string _selectedTheme = "DarkPurple";
    public string SelectedTheme
    {
        get => _selectedTheme;
        set { _selectedTheme = value; ThemeManager.ApplyTheme(value); _engine.Settings.Theme = value; OnPropertyChanged(); }
    }

    public ICommand StartStopCommand { get; }
    public ICommand AddSoundClipCommand { get; }
    public ICommand RemoveSoundClipCommand { get; }
    public ICommand PlayClipCommand { get; }
    public ICommand StopClipCommand { get; }
    public ICommand StopAllClipsCommand { get; }
    public ICommand RefreshDevicesCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ExportClipCommand { get; }
    public ICommand EditClipCommand { get; }

    public AudioEngine Engine => _engine;

    public MainViewModel()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _engine = new AudioEngine();

        _engine.InputLevelChanged += (s, level) => _dispatcher.BeginInvoke(() => InputLevel = level);
        _engine.OutputLevelChanged += (s, level) => _dispatcher.BeginInvoke(() => OutputLevel = level);
        _engine.ErrorOccurred += (s, msg) => _dispatcher.BeginInvoke(() => StatusMessage = $"Error: {msg}");
        _engine.ClipLoadErrorOccurred += (s, msg) => _dispatcher.BeginInvoke(() => StatusMessage = $"Warning: {msg}");
        _engine.RunningStateChanged += (s, running) => _dispatcher.BeginInvoke(() => { IsRunning = running; StatusMessage = running ? "Running" : "Stopped"; });
        _engine.SoundboardReinitialized += (s, e) => _dispatcher.BeginInvoke(() => ReloadSoundClipsList());

        StartStopCommand = new RelayCommand(ToggleStartStop);
        AddSoundClipCommand = new RelayCommand(AddSoundClip);
        RemoveSoundClipCommand = new RelayCommand<string>(RemoveSoundClip);
        PlayClipCommand = new RelayCommand<string>(PlayClip);
        StopClipCommand = new RelayCommand<string>(StopClip);
        StopAllClipsCommand = new RelayCommand(StopAllClips);
        RefreshDevicesCommand = new RelayCommand(RefreshDevices);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        ExportClipCommand = new RelayCommand<string>(ExportClip);
        EditClipCommand = new RelayCommand<string>(ToggleEditClip);
        TrimClipCommand = new RelayCommand<string>(TrimClip);

        RefreshDevices();
        InitializeEqualizer();
        ReloadSoundClipsList();

        _selectedTheme = _engine.Settings.Theme;
        ThemeManager.ApplyTheme(_selectedTheme);

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _uiTimer.Tick += (s, e) => UpdateUI();
        _uiTimer.Start();

        // Автосохранение каждые 10 секунд
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _autoSaveTimer.Tick += (s, e) =>
        {
            if (!_disposed)
                _engine.SaveSettings();
        };
        _autoSaveTimer.Start();
    }

    private void TrimClip(string? clipId)
    {
        if (clipId == null) return;
        var vm = SoundClips.FirstOrDefault(c => c.Id == clipId);
        if (vm == null) return;

        // Открываем окно обрезки — вызовется через code-behind
        TrimClipRequested?.Invoke(this, vm);
    }

    /// <summary>
    /// Событие для открытия окна обрезки (обрабатывается в MainWindow)
    /// </summary>
    public event EventHandler<SoundClipViewModel>? TrimClipRequested;

    /// <summary>
    /// Принудительное сохранение. Вызывается ДО Dispose при выходе.
    /// </summary>
    public void ForceSave()
    {
        if (_disposed) return;
        try
        {
            _engine.SaveSettings();
            System.Diagnostics.Debug.WriteLine("[MainViewModel] ForceSave completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] ForceSave error: {ex.Message}");
        }
    }

    private void InitializeEqualizer()
    {
        var bands = _engine.Equalizer.Bands;
        for (int i = 0; i < bands.Count; i++)
        {
            int index = i;
            EqualizerBands.Add(new EqualizerBandViewModel(bands[i], gain =>
            {
                _engine.Equalizer.SetBandGain(index, gain);
                if (index < _engine.Settings.EqualizerBands.Length)
                    _engine.Settings.EqualizerBands[index] = gain;
            }));
        }
    }

    private void ReloadSoundClipsList()
    {
        SoundClips.Clear();
        foreach (var clip in _engine.Soundboard.Clips)
            SoundClips.Add(new SoundClipViewModel(clip));
        OnPropertyChanged(nameof(StorageSize));
    }

    private void RefreshDevices()
    {
        InputDevices.Clear(); OutputDevices.Clear(); VirtualCableDevices.Clear();
        foreach (var dev in _engine.DeviceManager.GetInputDevices()) InputDevices.Add(dev);
        foreach (var dev in _engine.DeviceManager.GetOutputDevices())
        {
            OutputDevices.Add(dev);
            if (dev.IsVirtualCable) VirtualCableDevices.Add(dev);
        }
        SelectedInputDevice = InputDevices.FirstOrDefault(d => d.Id == _engine.Settings.InputDeviceId) ?? InputDevices.FirstOrDefault(d => d.IsDefault);
        SelectedOutputDevice = OutputDevices.FirstOrDefault(d => d.Id == _engine.Settings.OutputDeviceId) ?? OutputDevices.FirstOrDefault(d => d.IsDefault);
        SelectedVirtualCable = VirtualCableDevices.FirstOrDefault(d => d.Id == _engine.Settings.VirtualCableDeviceId) ?? VirtualCableDevices.FirstOrDefault();
    }

    private void ToggleStartStop() { if (IsRunning) _engine.Stop(); else _engine.Start(); }

    private void AddSoundClip()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select audio",
            Filter = "Audio|*.wav;*.mp3;*.ogg;*.flac;*.wma;*.aac|All|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                try
                {
                    var clip = _engine.Soundboard.AddClip(file);
                    SoundClips.Add(new SoundClipViewModel(clip));
                    OnPropertyChanged(nameof(StorageSize));

                    // Сохраняем СРАЗУ
                    _engine.SaveSettings();
                    StatusMessage = $"Added: {clip.Info.EffectiveName}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error: {Path.GetFileName(file)}: {ex.Message}";
                }
            }
        }
    }

    private void RemoveSoundClip(string? clipId)
    {
        if (clipId == null) return;
        _engine.Soundboard.RemoveClip(clipId);
        var vm = SoundClips.FirstOrDefault(c => c.Id == clipId);
        if (vm != null) SoundClips.Remove(vm);
        OnPropertyChanged(nameof(StorageSize));
        _engine.SaveSettings();
    }

    private void ExportClip(string? clipId)
    {
        if (clipId == null) return;
        var vm = SoundClips.FirstOrDefault(c => c.Id == clipId);
        if (vm == null) return;
        var dialog = new SaveFileDialog { FileName = vm.EffectiveName, Filter = "All|*.*", DefaultExt = Path.GetExtension(vm.Clip.Info.FilePath) };
        if (dialog.ShowDialog() == true)
        {
            try { _engine.Soundboard.ExportClip(clipId, dialog.FileName); StatusMessage = $"Exported: {vm.EffectiveName}"; }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }
    }

    private void ToggleEditClip(string? clipId)
    {
        if (clipId == null) return;
        var vm = SoundClips.FirstOrDefault(c => c.Id == clipId);
        if (vm != null)
        {
            vm.IsEditing = !vm.IsEditing;
            if (!vm.IsEditing) _engine.SaveSettings();
        }
    }

    private void PlayClip(string? clipId)
    {
        if (clipId != null) _engine.Soundboard.ToggleClip(clipId);
    }
    private void StopClip(string? clipId) { if (clipId != null) _engine.Soundboard.StopClip(clipId); }
    private void StopAllClips() { _engine.Soundboard.StopAll(); }
    private void SaveSettings() { _engine.SaveSettings(); StatusMessage = "Saved"; }

    private void UpdateUI()
    {
        foreach (var clip in SoundClips) clip.RefreshPlayState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _uiTimer.Stop();
        _autoSaveTimer.Stop();

        // НЕ вызываем SaveSettings здесь — уже вызвали в ForceSave
        _engine.Stop();
        _engine.Dispose();
    }
}