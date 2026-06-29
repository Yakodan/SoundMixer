namespace SoundMixer.Core.DSP;

public class GainProcessor : IDSPProcessor
{
    public string Name => "Gain";
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; } = 0;

    private float _gainDb = 0f;
    private float _gainLinear = 1f;

    public float GainDb
    {
        get => _gainDb;
        set
        {
            _gainDb = Math.Clamp(value, -60f, 30f);
            _gainLinear = (float)Math.Pow(10.0, _gainDb / 20.0);
        }
    }

    public float GainLinear => _gainLinear;

    public void Process(float[] buffer, int offset, int count, int sampleRate, int channels)
    {
        if (!IsEnabled || Math.Abs(_gainLinear - 1f) < 0.0001f) return;

        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] *= _gainLinear;
            if (buffer[i] > 1f) buffer[i] = (float)Math.Tanh(buffer[i]);
            else if (buffer[i] < -1f) buffer[i] = (float)Math.Tanh(buffer[i]);
        }
    }

    public void Reset() { }
}