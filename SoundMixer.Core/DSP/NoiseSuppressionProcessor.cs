namespace SoundMixer.Core.DSP;

public class NoiseSuppressionProcessor : IDSPProcessor
{
    public string Name => "Noise Suppression";
    public bool IsEnabled { get; set; } = false;
    public int Order { get; set; } = 1;

    public float SuppressionAmount { get; set; } = 0.5f;
    public bool IsLearning { get; private set; }

    private float _noiseFloor = 0.001f;
    private int _learnFrames;
    private int _learnFrameCount;
    private int _fftSize = 1024;

    public void StartLearning(int durationMs = 2000, int sampleRate = 48000)
    {
        IsLearning = true;
        _learnFrameCount = 0;
        _learnFrames = (int)(durationMs * sampleRate / 1000.0 / _fftSize);
    }

    public void Process(float[] buffer, int offset, int count, int sampleRate, int channels)
    {
        if (!IsEnabled) return;

        for (int i = offset; i < offset + count; i++)
        {
            float inputAbs = Math.Abs(buffer[i]);

            if (IsLearning)
            {
                _noiseFloor = Math.Max(_noiseFloor, inputAbs) * 0.999f + inputAbs * 0.001f;
                _learnFrameCount++;
                if (_learnFrameCount >= _learnFrames * _fftSize)
                {
                    IsLearning = false;
                    _noiseFloor *= 1.5f;
                }
            }

            if (inputAbs < _noiseFloor * SuppressionAmount * 2f)
            {
                float attenuation = inputAbs / (_noiseFloor * SuppressionAmount * 2f);
                attenuation = attenuation * attenuation;
                buffer[i] *= attenuation;
            }
        }
    }

    public void Reset()
    {
        _noiseFloor = 0.001f;
        IsLearning = false;
    }
}