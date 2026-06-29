namespace SoundMixer.Core.Models;

public class EqualizerBand
{
    public float Frequency { get; set; }
    public float GainDb { get; set; }
    public float Bandwidth { get; set; } = 1f;

    public string Label => Frequency >= 1000 ? $"{Frequency / 1000:0.#}K" : $"{Frequency:0}";
}