namespace SoundMixer.Core.DSP;

public class CompressorProcessor : IDSPProcessor
{
    public string Name => "Compressor";
    public bool IsEnabled { get; set; } = false;
    public int Order { get; set; } = 3;

    public float ThresholdDb { get; set; } = -20f;
    public float Ratio { get; set; } = 4f;
    public float AttackMs { get; set; } = 5f;
    public float ReleaseMs { get; set; } = 50f;
    public float MakeupGainDb { get; set; } = 0f;

    private float _envelope = 0f;

    public void Process(float[] buffer, int offset, int count, int sampleRate, int channels)
    {
        if (!IsEnabled) return;

        float threshold = (float)Math.Pow(10.0, ThresholdDb / 20.0);
        float makeupGain = (float)Math.Pow(10.0, MakeupGainDb / 20.0);
        float attackCoeff = (float)Math.Exp(-1.0 / (AttackMs * 0.001 * sampleRate));
        float releaseCoeff = (float)Math.Exp(-1.0 / (ReleaseMs * 0.001 * sampleRate));

        for (int i = offset; i < offset + count; i++)
        {
            float inputAbs = Math.Abs(buffer[i]);

            if (inputAbs > _envelope)
                _envelope = attackCoeff * _envelope + (1f - attackCoeff) * inputAbs;
            else
                _envelope = releaseCoeff * _envelope + (1f - releaseCoeff) * inputAbs;

            float gain = 1f;
            if (_envelope > threshold && _envelope > 0.00001f)
            {
                float dbOver = 20f * (float)Math.Log10(_envelope / threshold);
                float dbReduction = dbOver * (1f - 1f / Ratio);
                gain = (float)Math.Pow(10.0, -dbReduction / 20.0);
            }

            buffer[i] *= gain * makeupGain;
        }
    }

    public void Reset()
    {
        _envelope = 0f;
    }
}