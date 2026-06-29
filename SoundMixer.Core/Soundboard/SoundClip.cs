using NAudio.Wave;
using SoundMixer.Core.Models;

namespace SoundMixer.Core.Soundboard;

public class SoundClip : IDisposable
{
    public SoundClipInfo Info { get; }

    private float[]? _fullAudioData;    // Полные данные (без обрезки)
    private int _sampleRate;
    private int _channels;
    private long _position;
    private bool _isPlaying;

    // Границы воспроизведения в сэмплах
    private int _playStartSample;
    private int _playEndSample;

    public bool IsPlaying => _isPlaying;
    public bool IsLoaded => _fullAudioData != null;

    /// <summary>Полная длительность клипа</summary>
    public TimeSpan FullDuration { get; private set; }

    /// <summary>Длительность с учётом обрезки</summary>
    public TimeSpan TrimmedDuration
    {
        get
        {
            if (_sampleRate <= 0 || _channels <= 0) return TimeSpan.Zero;
            int trimmedSamples = _playEndSample - _playStartSample;
            return TimeSpan.FromSeconds((double)trimmedSamples / _sampleRate / _channels);
        }
    }

    public TimeSpan CurrentPosition => _sampleRate > 0 && _channels > 0
        ? TimeSpan.FromSeconds((double)_position / _sampleRate / _channels)
        : TimeSpan.Zero;

    public int SampleRate => _sampleRate;
    public int Channels => _channels;
    public int TotalSamples => _fullAudioData?.Length ?? 0;

    public SoundClip(SoundClipInfo info)
    {
        Info = info;
    }

    public void Load(int targetSampleRate = 48000, int targetChannels = 1)
    {
        if (!File.Exists(Info.FilePath))
            throw new FileNotFoundException($"Файл не найден: {Info.FilePath}");

        _sampleRate = targetSampleRate;
        _channels = targetChannels;

        using var reader = new AudioFileReader(Info.FilePath);

        bool needResample = reader.WaveFormat.SampleRate != targetSampleRate;
        bool needChannelConvert = reader.WaveFormat.Channels != targetChannels;

        if (needResample || needChannelConvert)
        {
            var targetFormat = new WaveFormat(targetSampleRate, 16, targetChannels);
            using var resampler = new MediaFoundationResampler(reader, targetFormat);
            resampler.ResamplerQuality = 60;
            var sampleProvider = resampler.ToSampleProvider();
            _fullAudioData = ReadAllSamples(sampleProvider, targetSampleRate * targetChannels);
        }
        else
        {
            _fullAudioData = ReadAllSamples(reader, _sampleRate * _channels);
        }

        FullDuration = (_sampleRate > 0 && _channels > 0)
            ? TimeSpan.FromSeconds((double)_fullAudioData.Length / _sampleRate / _channels)
            : TimeSpan.Zero;

        // Применяем сохранённые границы обрезки
        RecalculateTrimBounds();
    }

    private static float[] ReadAllSamples(ISampleProvider provider, int bufferSize)
    {
        var allData = new List<float>();
        var buffer = new float[bufferSize];
        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
                allData.Add(buffer[i]);
        }
        return allData.ToArray();
    }

    /// <summary>
    /// Пересчитывает границы воспроизведения из секунд в сэмплы.
    /// </summary>
    public void RecalculateTrimBounds()
    {
        if (_fullAudioData == null) return;

        int totalSamples = _fullAudioData.Length;
        int samplesPerSecond = _sampleRate * _channels;

        // Минимальная длина воспроизведения — 1 сэмпл на канал
        int minPlayLength = _channels;

        // Начало
        int maxStartSample = Math.Max(0, totalSamples - minPlayLength);
        _playStartSample = (int)(Info.TrimStart * samplesPerSecond);
        _playStartSample = Math.Clamp(_playStartSample, 0, maxStartSample);

        // Конец
        if (Info.TrimEnd <= 0 || Info.TrimEnd >= FullDuration.TotalSeconds)
        {
            _playEndSample = totalSamples;
        }
        else
        {
            _playEndSample = (int)(Info.TrimEnd * samplesPerSecond);
        }

        _playEndSample = Math.Clamp(_playEndSample, _playStartSample + minPlayLength, totalSamples);

        // Финальная гарантия что диапазон ненулевой
        if (_playEndSample <= _playStartSample)
            _playEndSample = totalSamples;
    }

    /// <summary>
    /// Устанавливает обрезку в секундах. Автоматически валидирует значения.
    /// </summary>
    public void SetTrim(double startSeconds, double endSeconds)
    {
        double maxSeconds = FullDuration.TotalSeconds;

        startSeconds = Math.Clamp(startSeconds, 0, maxSeconds - 0.05);
        endSeconds = Math.Clamp(endSeconds, startSeconds + 0.05, maxSeconds);

        Info.TrimStart = startSeconds;
        Info.TrimEnd = endSeconds;

        RecalculateTrimBounds();
    }

    /// <summary>
    /// Сбрасывает обрезку — воспроизводится весь клип.
    /// </summary>
    public void ResetTrim()
    {
        Info.TrimStart = 0;
        Info.TrimEnd = 0;
        RecalculateTrimBounds();
    }

    public void Play()
    {
        _position = _playStartSample;
        _isPlaying = true;
    }

    public void Stop()
    {
        _isPlaying = false;
        _position = _playStartSample;
    }

    public void Pause()
    {
        _isPlaying = false;
    }

    public int ReadSamples(float[] buffer, int offset, int count)
    {
        if (!_isPlaying || _fullAudioData == null) return 0;

        int samplesRead = 0;
        for (int i = 0; i < count; i++)
        {
            if (_position >= _playEndSample)
            {
                if (Info.Loop)
                    _position = _playStartSample;
                else
                {
                    _isPlaying = false;
                    break;
                }
            }

            buffer[offset + i] = _fullAudioData[_position] * Info.Volume;
            _position++;
            samplesRead++;
        }

        return samplesRead;
    }

    /// <summary>
    /// Получить ВСЕ аудиоданные (для визуализации waveform).
    /// </summary>
    public float[]? GetFullAudioData() => _fullAudioData;

    /// <summary>
    /// Получить waveform данные для визуализации (downsampled).
    /// </summary>
    public float[] GetWaveformData(int targetPoints)
    {
        if (_fullAudioData == null || _fullAudioData.Length == 0)
            return Array.Empty<float>();

        if (_fullAudioData.Length <= targetPoints)
            return (float[])_fullAudioData.Clone();

        float[] result = new float[targetPoints];
        float samplesPerPoint = (float)_fullAudioData.Length / targetPoints;

        for (int i = 0; i < targetPoints; i++)
        {
            int start = (int)(i * samplesPerPoint);
            int end = Math.Min((int)((i + 1) * samplesPerPoint), _fullAudioData.Length);

            float maxVal = 0;
            for (int j = start; j < end; j++)
            {
                float abs = Math.Abs(_fullAudioData[j]);
                if (abs > maxVal) maxVal = abs;
            }
            result[i] = maxVal;
        }

        return result;
    }

    /// <summary>
    /// Если играет — останавливает. Если не играет — запускает.
    /// </summary>
    public void TogglePlay()
    {
        if (_isPlaying)
            Stop();
        else
            Play();
    }

    public void Dispose()
    {
        _fullAudioData = null;
    }
}