namespace SoundMixer.Core.Audio;

public static class AudioMixer
{
    public static void Mix(float[] buffer1, float volume1,
                           float[] buffer2, float volume2,
                           float[] output, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float s1 = i < buffer1.Length ? buffer1[i] * volume1 : 0f;
            float s2 = i < buffer2.Length ? buffer2[i] * volume2 : 0f;
            output[i] = s1 + s2;

            if (output[i] > 1f) output[i] = (float)Math.Tanh(output[i]);
            else if (output[i] < -1f) output[i] = (float)Math.Tanh(output[i]);
        }
    }

    public static void MixInto(float[] destination, float[] source, float volume, int count)
    {
        int len = Math.Min(count, Math.Min(destination.Length, source.Length));
        for (int i = 0; i < len; i++)
        {
            destination[i] += source[i] * volume;
            if (destination[i] > 1f) destination[i] = (float)Math.Tanh(destination[i]);
            else if (destination[i] < -1f) destination[i] = (float)Math.Tanh(destination[i]);
        }
    }

    public static void ApplyVolume(float[] buffer, float volume, int count)
    {
        if (Math.Abs(volume - 1f) < 0.0001f) return;
        for (int i = 0; i < count; i++)
        {
            buffer[i] *= volume;
        }
    }
}