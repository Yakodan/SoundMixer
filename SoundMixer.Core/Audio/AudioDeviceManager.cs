using NAudio.CoreAudioApi;

namespace SoundMixer.Core.Audio;

public class AudioDeviceManager : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator;

    public AudioDeviceManager()
    {
        _enumerator = new MMDeviceEnumerator();
    }

    public List<AudioDeviceInfo> GetInputDevices()
    {
        var devices = new List<AudioDeviceInfo>();
        var collection = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

        string defaultId = "";
        try
        {
            defaultId = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).ID;
        }
        catch { }

        foreach (var device in collection)
        {
            devices.Add(new AudioDeviceInfo
            {
                Id = device.ID,
                Name = device.FriendlyName,
                IsDefault = device.ID == defaultId
            });
        }

        return devices;
    }

    public List<AudioDeviceInfo> GetOutputDevices()
    {
        var devices = new List<AudioDeviceInfo>();
        var collection = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        string defaultId = "";
        try
        {
            defaultId = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications).ID;
        }
        catch { }

        foreach (var device in collection)
        {
            devices.Add(new AudioDeviceInfo
            {
                Id = device.ID,
                Name = device.FriendlyName,
                IsDefault = device.ID == defaultId,
                IsVirtualCable = device.FriendlyName.Contains("CABLE", StringComparison.OrdinalIgnoreCase) ||
                                 device.FriendlyName.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                                 device.FriendlyName.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase)
            });
        }

        return devices;
    }

    public MMDevice? GetDeviceById(string deviceId)
    {
        try
        {
            return _enumerator.GetDevice(deviceId);
        }
        catch
        {
            return null;
        }
    }

    public AudioDeviceInfo? FindVirtualCable()
    {
        return GetOutputDevices().FirstOrDefault(d => d.IsVirtualCable);
    }

    public void Dispose()
    {
        _enumerator?.Dispose();
    }
}