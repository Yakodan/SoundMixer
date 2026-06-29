namespace SoundMixer.Core.DSP;

public class BiQuadFilter
{
    private readonly float _b0, _b1, _b2, _a1, _a2;
    private float _x1, _x2, _y1, _y2;

    private BiQuadFilter(float b0, float b1, float b2, float a0, float a1, float a2)
    {
        // Защита от деления на ноль
        if (Math.Abs(a0) < float.Epsilon)
            a0 = 1f;

        _b0 = b0 / a0;
        _b1 = b1 / a0;
        _b2 = b2 / a0;
        _a1 = a1 / a0;
        _a2 = a2 / a0;
    }

    public float Transform(float input)
    {
        float output = _b0 * input + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;

        _x2 = _x1;
        _x1 = input;
        _y2 = _y1;
        _y1 = output;

        // Защита от NaN/Infinity — сбрасываем состояние
        if (float.IsNaN(output) || float.IsInfinity(output))
        {
            _x1 = 0f;
            _x2 = 0f;
            _y1 = 0f;
            _y2 = 0f;
            return input; // Возвращаем исходный сигнал
        }

        return output;
    }

    public static BiQuadFilter PeakingEQ(float sampleRate, float frequency, float bandwidth, float gainDb)
    {
        // Если усиление около нуля — возвращаем pass-through фильтр
        if (Math.Abs(gainDb) < 0.01f)
            return new BiQuadFilter(1, 0, 0, 1, 0, 0);

        // Защита от некорректных параметров
        if (sampleRate <= 0) sampleRate = 48000;
        if (frequency <= 0) frequency = 1000;
        if (frequency >= sampleRate / 2f) frequency = sampleRate / 2f - 1;
        if (bandwidth <= 0) bandwidth = 1f;

        float A = (float)Math.Pow(10.0, gainDb / 40.0);
        float w0 = 2f * MathF.PI * frequency / sampleRate;
        float sinW0 = MathF.Sin(w0);
        float cosW0 = MathF.Cos(w0);

        // Защита от деления на ноль в sinh
        if (Math.Abs(sinW0) < float.Epsilon)
            return new BiQuadFilter(1, 0, 0, 1, 0, 0);

        float alpha = sinW0 * (float)Math.Sinh(Math.Log(2.0) / 2.0 * bandwidth * w0 / sinW0);

        // Защита от нулевого alpha
        if (Math.Abs(alpha) < float.Epsilon)
            return new BiQuadFilter(1, 0, 0, 1, 0, 0);

        return new BiQuadFilter(
            1 + alpha * A,
            -2 * cosW0,
            1 - alpha * A,
            1 + alpha / A,
            -2 * cosW0,
            1 - alpha / A
        );
    }

    public static BiQuadFilter LowPass(float sampleRate, float frequency, float q = 0.707f)
    {
        if (sampleRate <= 0) sampleRate = 48000;
        if (frequency <= 0) frequency = 1000;
        if (frequency >= sampleRate / 2f) frequency = sampleRate / 2f - 1;
        if (q <= 0) q = 0.707f;

        float w0 = 2f * MathF.PI * frequency / sampleRate;
        float cosW0 = MathF.Cos(w0);
        float sinW0 = MathF.Sin(w0);
        float alpha = sinW0 / (2f * q);

        if (Math.Abs(alpha) < float.Epsilon)
            return new BiQuadFilter(1, 0, 0, 1, 0, 0);

        return new BiQuadFilter(
            (1 - cosW0) / 2,
            1 - cosW0,
            (1 - cosW0) / 2,
            1 + alpha,
            -2 * cosW0,
            1 - alpha
        );
    }

    public static BiQuadFilter HighPass(float sampleRate, float frequency, float q = 0.707f)
    {
        if (sampleRate <= 0) sampleRate = 48000;
        if (frequency <= 0) frequency = 1000;
        if (frequency >= sampleRate / 2f) frequency = sampleRate / 2f - 1;
        if (q <= 0) q = 0.707f;

        float w0 = 2f * MathF.PI * frequency / sampleRate;
        float cosW0 = MathF.Cos(w0);
        float sinW0 = MathF.Sin(w0);
        float alpha = sinW0 / (2f * q);

        if (Math.Abs(alpha) < float.Epsilon)
            return new BiQuadFilter(1, 0, 0, 1, 0, 0);

        return new BiQuadFilter(
            (1 + cosW0) / 2,
            -(1 + cosW0),
            (1 + cosW0) / 2,
            1 + alpha,
            -2 * cosW0,
            1 - alpha
        );
    }
}