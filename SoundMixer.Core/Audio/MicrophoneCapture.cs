using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace SoundMixer.Core.Audio;

public class MicrophoneCapture : IDisposable
{
    private WasapiCapture? _capture;
    private readonly AudioDeviceManager _deviceManager;

    public event EventHandler<float[]>? DataAvailable;
    public event EventHandler<float>? LevelChanged;

    public bool IsCapturing { get; private set; }
    public WaveFormat? WaveFormat => _capture?.WaveFormat;

    public MicrophoneCapture(AudioDeviceManager deviceManager)
    {
        _deviceManager = deviceManager;
    }

    /// <summary>
    /// Захват с минимальной задержкой — буфер 5ms
    /// </summary>
    public void StartWithWasapiFormat(string deviceId)
    {
        Stop();

        var device = _deviceManager.GetDeviceById(deviceId);
        if (device == null)
            throw new InvalidOperationException($"Устройство не найдено: {deviceId}");

        // Используем WasapiCapture с минимальным буфером
        _capture = new WasapiCapture(device, true, 5); // 5ms latency
        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;
        _capture.StartRecording();
        IsCapturing = true;

        System.Diagnostics.Debug.WriteLine(
            $"[MicCapture] Захват запущен: {_capture.WaveFormat?.SampleRate} Hz, " +
            $"{_capture.WaveFormat?.Channels} ch, 5ms buffer");
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0 || _capture == null) return;

        var format = _capture.WaveFormat;
        int bytesPerSample = format.BitsPerSample / 8;
        int sampleCount = e.BytesRecorded / bytesPerSample;
        float[] floatBuffer = new float[sampleCount];

        // Быстрая конвертация в float
        if (format.BitsPerSample == 32 && format.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            Buffer.BlockCopy(e.Buffer, 0, floatBuffer, 0, e.BytesRecorded);
        }
        else if (format.BitsPerSample == 16)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i * 2);
                floatBuffer[i] = sample / 32768f;
            }
        }
        else if (format.BitsPerSample == 32)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                int sample = BitConverter.ToInt32(e.Buffer, i * 4);
                floatBuffer[i] = sample / (float)int.MaxValue;
            }
        }

        // Уровень
        float maxLevel = 0;
        for (int i = 0; i < floatBuffer.Length; i++)
        {
            float abs = Math.Abs(floatBuffer[i]);
            if (abs > maxLevel) maxLevel = abs;
        }
        LevelChanged?.Invoke(this, maxLevel);
        DataAvailable?.Invoke(this, floatBuffer);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        IsCapturing = false;
    }

    public void Stop()
    {
        if (_capture != null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;

            if (IsCapturing)
            {
                try { _capture.StopRecording(); }
                catch { }
            }

            _capture.Dispose();
            _capture = null;
        }
        IsCapturing = false;
    }

    public void Dispose()
    {
        Stop();
    }
}