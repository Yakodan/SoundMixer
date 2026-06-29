namespace SoundMixer.Core.DSP;

public interface IDSPProcessor
{
    string Name { get; }
    bool IsEnabled { get; set; }
    int Order { get; set; }
    void Process(float[] buffer, int offset, int count, int sampleRate, int channels);
    void Reset();
}