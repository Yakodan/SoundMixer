using NAudio.Wave;

namespace SoundMixer.Core.Soundboard;

public static class SoundClipEditor
{
    public static float[] Normalize(float[] data)
    {
        float maxAmp = 0;
        for (int i = 0; i < data.Length; i++)
        {
            float abs = Math.Abs(data[i]);
            if (abs > maxAmp) maxAmp = abs;
        }

        if (maxAmp < 0.0001f) return data;

        float[] result = new float[data.Length];
        float gain = 1f / maxAmp;
        for (int i = 0; i < data.Length; i++)
            result[i] = data[i] * gain;

        return result;
    }

    public static void ApplyFadeIn(float[] data, int sampleRate, float durationMs)
    {
        int fadeSamples = Math.Min((int)(durationMs * sampleRate / 1000), data.Length);
        for (int i = 0; i < fadeSamples; i++)
            data[i] *= (float)i / fadeSamples;
    }

    public static void ApplyFadeOut(float[] data, int sampleRate, float durationMs)
    {
        int fadeSamples = Math.Min((int)(durationMs * sampleRate / 1000), data.Length);
        int startIndex = data.Length - fadeSamples;
        for (int i = 0; i < fadeSamples; i++)
            data[startIndex + i] *= 1f - (float)i / fadeSamples;
    }

    public static void ApplyGain(float[] data, float gainDb)
    {
        float gainLinear = (float)Math.Pow(10.0, gainDb / 20.0);
        for (int i = 0; i < data.Length; i++)
            data[i] *= gainLinear;
    }

    public static float[] Trim(float[] data, int sampleRate, int channels,
                                double startSeconds, double endSeconds)
    {
        int startSample = Math.Clamp((int)(startSeconds * sampleRate * channels), 0, data.Length);
        int endSample = endSeconds > 0
            ? Math.Clamp((int)(endSeconds * sampleRate * channels), startSample, data.Length)
            : data.Length;

        int length = endSample - startSample;
        float[] result = new float[length];
        Array.Copy(data, startSample, result, 0, length);
        return result;
    }

    public static void ExportToWav(float[] data, int sampleRate, int channels, string outputPath)
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        using var writer = new WaveFileWriter(outputPath, format);
        writer.WriteSamples(data, 0, data.Length);
    }

    public static float[] GetWaveformData(float[] data, int targetPoints)
    {
        if (data.Length <= targetPoints)
            return (float[])data.Clone();

        float[] result = new float[targetPoints];
        float samplesPerPoint = (float)data.Length / targetPoints;

        for (int i = 0; i < targetPoints; i++)
        {
            int start = (int)(i * samplesPerPoint);
            int end = Math.Min((int)((i + 1) * samplesPerPoint), data.Length);

            float maxVal = 0;
            for (int j = start; j < end; j++)
            {
                float abs = Math.Abs(data[j]);
                if (abs > maxVal) maxVal = abs;
            }
            result[i] = maxVal;
        }

        return result;
    }
}