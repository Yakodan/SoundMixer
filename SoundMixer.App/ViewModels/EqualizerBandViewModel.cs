using System.ComponentModel;
using System.Runtime.CompilerServices;
using SoundMixer.Core.Models;

namespace SoundMixer.App.ViewModels;

public class EqualizerBandViewModel : INotifyPropertyChanged
{
    private readonly EqualizerBand _band;
    private readonly Action<float> _onGainChanged;

    public string Label => _band.Label;

    public float GainDb
    {
        get => _band.GainDb;
        set
        {
            _band.GainDb = value;
            _onGainChanged(value);
            OnPropertyChanged();
        }
    }

    public EqualizerBandViewModel(EqualizerBand band, Action<float> onGainChanged)
    {
        _band = band;
        _onGainChanged = onGainChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}