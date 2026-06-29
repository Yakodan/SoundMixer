using System.ComponentModel;
using System.Runtime.CompilerServices;
using SoundMixer.Core.Soundboard;

namespace SoundMixer.App.ViewModels;

public class SoundClipViewModel : INotifyPropertyChanged
{
    private readonly SoundClip _clip;

    public string Id => _clip.Info.Id;
    public SoundClip Clip => _clip;

    public string Duration => _clip.TrimmedDuration.TotalSeconds > 0
        ? FormatDuration(_clip.TrimmedDuration)
        : FormatDuration(_clip.FullDuration);

    public string FullDuration => FormatDuration(_clip.FullDuration);

    public bool IsTrimmed => _clip.Info.TrimStart > 0 ||
                             (_clip.Info.TrimEnd > 0 && _clip.Info.TrimEnd < _clip.FullDuration.TotalSeconds);

    public string TrimInfo => IsTrimmed
        ? $"✂️ {FormatSeconds(_clip.Info.TrimStart)} — {FormatSeconds(_clip.Info.TrimEnd > 0 ? _clip.Info.TrimEnd : _clip.FullDuration.TotalSeconds)}"
        : "";

    public string EffectiveName => _clip.Info.EffectiveName;

    public string Emoji
    {
        get => _clip.Info.Emoji;
        set { _clip.Info.Emoji = value; OnPropertyChanged(); }
    }

    public string Name
    {
        get => _clip.Info.DisplayName ?? _clip.Info.Name;
        set
        {
            _clip.Info.DisplayName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            OnPropertyChanged();
            OnPropertyChanged(nameof(EffectiveName));
        }
    }

    public bool IsPlaying => _clip.IsPlaying;

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set { _isEditing = value; OnPropertyChanged(); }
    }

    public float Volume
    {
        get => _clip.Info.Volume;
        set { _clip.Info.Volume = value; OnPropertyChanged(); }
    }

    public bool Loop
    {
        get => _clip.Info.Loop;
        set { _clip.Info.Loop = value; OnPropertyChanged(); }
    }

    public string Color
    {
        get => _clip.Info.Color;
        set { _clip.Info.Color = value; OnPropertyChanged(); }
    }

    public string HotKeyDisplay => string.IsNullOrEmpty(_clip.Info.HotKey) ? "Нет" : _clip.Info.HotKey;

    public string? HotKeyRaw
    {
        get => _clip.Info.HotKey;
        set { _clip.Info.HotKey = value; OnPropertyChanged(); OnPropertyChanged(nameof(HotKeyDisplay)); }
    }

    private bool _isRecordingHotKey;
    public bool IsRecordingHotKey
    {
        get => _isRecordingHotKey;
        set { _isRecordingHotKey = value; OnPropertyChanged(); }
    }

    public SoundClipViewModel(SoundClip clip)
    {
        _clip = clip;
    }

    public void RefreshPlayState()
    {
        OnPropertyChanged(nameof(IsPlaying));
    }

    public void RefreshTrimInfo()
    {
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(IsTrimmed));
        OnPropertyChanged(nameof(TrimInfo));
    }

    private static string FormatDuration(TimeSpan ts)
    {
        return ts.TotalMinutes >= 1 ? ts.ToString(@"m\:ss") : $"{ts.TotalSeconds:F1}с";
    }

    private static string FormatSeconds(double s)
    {
        return s < 60 ? $"{s:F1}с" : TimeSpan.FromSeconds(s).ToString(@"m\:ss\.f");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}