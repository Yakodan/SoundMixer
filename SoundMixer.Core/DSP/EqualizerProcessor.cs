using SoundMixer.Core.Models;

namespace SoundMixer.Core.DSP;

public class EqualizerProcessor : IDSPProcessor
{
    public string Name => "Equalizer";
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; } = 2;

    private readonly List<EqualizerBand> _bands;
    private volatile BiQuadFilter[]? _filters;  // volatile для потокобезопасности
    private int _currentSampleRate;
    private readonly object _lock = new();

    private static readonly float[] DefaultFrequencies =
    {
        31f, 62f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f
    };

    public IReadOnlyList<EqualizerBand> Bands => _bands.AsReadOnly();

    public EqualizerProcessor()
    {
        _bands = new List<EqualizerBand>();
        foreach (var freq in DefaultFrequencies)
        {
            _bands.Add(new EqualizerBand
            {
                Frequency = freq,
                GainDb = 0f,
                Bandwidth = 1f
            });
        }
    }

    public void SetBandGain(int bandIndex, float gainDb)
    {
        if (bandIndex < 0 || bandIndex >= _bands.Count) return;
        _bands[bandIndex].GainDb = Math.Clamp(gainDb, -15f, 15f);

        // Пересоздаём только если знаем sampleRate
        if (_currentSampleRate > 0)
            RebuildFilters(_currentSampleRate);
    }

    public void SetAllBands(float[] gains)
    {
        for (int i = 0; i < Math.Min(gains.Length, _bands.Count); i++)
        {
            _bands[i].GainDb = Math.Clamp(gains[i], -15f, 15f);
        }

        if (_currentSampleRate > 0)
            RebuildFilters(_currentSampleRate);
    }

    private void RebuildFilters(int sampleRate)
    {
        if (sampleRate <= 0) return;

        lock (_lock)
        {
            _currentSampleRate = sampleRate;

            // Создаём новый массив, а потом атомарно присваиваем
            var newFilters = new BiQuadFilter[_bands.Count];
            for (int i = 0; i < _bands.Count; i++)
            {
                newFilters[i] = BiQuadFilter.PeakingEQ(
                    sampleRate,
                    _bands[i].Frequency,
                    _bands[i].Bandwidth,
                    _bands[i].GainDb
                );
            }

            // Атомарная замена ссылки
            _filters = newFilters;
        }
    }

    public void Process(float[] buffer, int offset, int count, int sampleRate, int channels)
    {
        if (!IsEnabled) return;
        if (sampleRate <= 0) return;

        // Если sampleRate изменился или фильтры не созданы — пересоздаём
        if (_filters == null || _currentSampleRate != sampleRate)
        {
            RebuildFilters(sampleRate);
        }

        // Берём локальную ссылку — защита от замены в другом потоке
        var filters = _filters;
        if (filters == null || filters.Length == 0) return;

        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];
            for (int f = 0; f < filters.Length; f++)
            {
                var filter = filters[f];
                if (filter != null)
                    sample = filter.Transform(sample);
            }
            buffer[i] = sample;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _filters = null;
            _currentSampleRate = 0;
        }
    }
}