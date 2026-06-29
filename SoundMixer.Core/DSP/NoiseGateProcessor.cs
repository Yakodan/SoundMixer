namespace SoundMixer.Core.DSP;

public class NoiseGateProcessor : IDSPProcessor
{
    public string Name => "Noise Gate";
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; } = 1;

    public float ThresholdDb { get; set; } = -40f;
    public float AttackMs { get; set; } = 1f;
    public float ReleaseMs { get; set; } = 100f;
    public float HoldMs { get; set; } = 50f;

    private float _envelope = 0f;
    private float _gateGain = 0f;
    private int _holdCounter = 0;
    private bool _isOpen = false;

    public void Process(float[] buffer, int offset, int count, int sampleRate, int channels)
    {
        if (!IsEnabled) return;

        float thresholdLinear = (float)Math.Pow(10.0, ThresholdDb / 20.0);
        float attackCoeff = (float)Math.Exp(-1.0 / (AttackMs * 0.001 * sampleRate));
        float releaseCoeff = (float)Math.Exp(-1.0 / (ReleaseMs * 0.001 * sampleRate));
        int holdSamples = (int)(HoldMs * 0.001f * sampleRate);

        for (int i = offset; i < offset + count; i++)
        {
            float inputLevel = Math.Abs(buffer[i]);

            if (inputLevel > _envelope)
                _envelope = attackCoeff * _envelope + (1f - attackCoeff) * inputLevel;
            else
                _envelope = releaseCoeff * _envelope + (1f - releaseCoeff) * inputLevel;

            if (_envelope >= thresholdLinear)
            {
                _isOpen = true;
                _holdCounter = holdSamples;
            }
            else if (_holdCounter > 0)
            {
                _holdCounter--;
            }
            else
            {
                _isOpen = false;
            }

            float targetGain = _isOpen ? 1f : 0f;
            float smoothCoeff = _isOpen ? (1f - attackCoeff) : (1f - releaseCoeff);
            _gateGain += smoothCoeff * (targetGain - _gateGain);

            buffer[i] *= _gateGain;
        }
    }

    public void Reset()
    {
        _envelope = 0f;
        _gateGain = 0f;
        _holdCounter = 0;
        _isOpen = false;
    }
}