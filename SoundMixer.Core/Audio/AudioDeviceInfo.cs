namespace SoundMixer.Core.Audio;

public class AudioDeviceInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsDefault { get; set; }
    public bool IsVirtualCable { get; set; }

    public override string ToString() => Name;
}