using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace SoundMixer.Core.Audio;

public class VirtualOutputWriter : IDisposable
{
    private WasapiOut? _wasapiOut;
    private BufferedWaveProvider? _bufferedProvider;
    private readonly AudioDeviceManager _deviceManager;

    public bool IsWriting { get; private set; }

    public VirtualOutputWriter(AudioDeviceManager deviceManager)
    {
        _deviceManager = deviceManager;
    }

    public void Start(string deviceId, int sampleRate, int channels)
    {
        Stop();

        var device = _deviceManager.GetDeviceById(deviceId);
        if (device == null)
            throw new InvalidOperationException($"Виртуальный кабель не найден: {deviceId}");

        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _bufferedProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferLength = sampleRate * channels * 4 * 2,
            DiscardOnBufferOverflow = true
        };

        // 10мс задержка для минимальной латентности
        _wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, true, 10);
        _wasapiOut.Init(_bufferedProvider);
        _wasapiOut.Play();
        IsWriting = true;
    }

    public void WriteSamples(float[] buffer, int offset, int count)
    {
        if (!IsWriting || _bufferedProvider == null) return;

        byte[] byteBuffer = new byte[count * 4];
        Buffer.BlockCopy(buffer, offset * 4, byteBuffer, 0, count * 4);

        try
        {
            _bufferedProvider.AddSamples(byteBuffer, 0, byteBuffer.Length);
        }
        catch (InvalidOperationException)
        {
            // Buffer overflow — пропускаем
        }
    }

    public void Stop()
    {
        if (_wasapiOut != null)
        {
            try { _wasapiOut.Stop(); }
            catch { }
            _wasapiOut.Dispose();
            _wasapiOut = null;
        }
        _bufferedProvider = null;
        IsWriting = false;
    }

    public void Dispose()
    {
        Stop();
    }
}